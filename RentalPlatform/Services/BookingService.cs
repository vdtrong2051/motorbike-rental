using RentalPlatform.Models.Entities;
using RentalPlatform.Models.ViewModels.Booking;
using RentalPlatform.Repositories;
using RentalPlatform.Data;
using RentalPlatform.Services.Availability;
using Microsoft.EntityFrameworkCore;
using System.Data;
using RentalPlatform.Services.Lifecycle;

namespace RentalPlatform.Services;

public class BookingService : IBookingService
{
    private readonly IBookingRepository _bookingRepository;
    private readonly IVehicleRepository _vehicleRepository;
    private readonly IContractService _contractService;
    private readonly IPaymentRepository _paymentRepository;
    private readonly AppDbContext _db;
    private readonly IVehicleAvailabilityService _availability;

    public BookingService(
        IBookingRepository bookingRepository,
        IVehicleRepository vehicleRepository,
        IContractService contractService,
        IPaymentRepository paymentRepository,
        AppDbContext db,
        IVehicleAvailabilityService availability)
    {
        _bookingRepository = bookingRepository;
        _vehicleRepository = vehicleRepository;
        _contractService = contractService;
        _paymentRepository = paymentRepository;
        _db = db;
        _availability = availability;
    }

    public async Task<bool> KiemTraConTrongAsync(Guid vehicleId, DateOnly tuNgay, DateOnly denNgay, Guid? loaiTruBookingId = null)
    {
        if (denNgay <= tuNgay) return false;
        var window = AvailabilityWindow.FromLegacy(tuNgay, denNgay);
        return !await _availability.IsBusyAsync(vehicleId, window.Start, window.End,
            excludedBookingId: loaiTruBookingId);
    }

    public decimal TinhTongTien(decimal giaMotNgay, DateOnly ngayBatDau, DateOnly ngayKetThuc)
    {
        var soNgay = ngayKetThuc.DayNumber - ngayBatDau.DayNumber;
        return giaMotNgay * Math.Max(soNgay, 1);
    }

    public async Task<BookingOperationResult> CreateAsync(BookingCreateViewModel model, Guid customerId)
    {
        if (model.NgayKetThuc <= model.NgayBatDau)
            return BookingOperationResult.Fail("Ngày trả xe phải sau ngày nhận xe.");

        if (model.NgayBatDau < DateOnly.FromDateTime(RentalLifecyclePolicy.VietnamNow()))
            return BookingOperationResult.Fail("Ngày nhận xe không được ở trong quá khứ.");

        var vehicle = await _vehicleRepository.GetByIdAsync(model.VehicleId, includeImages: false);
        if (vehicle is null) return BookingOperationResult.Fail("Không tìm thấy xe.");

        if (vehicle.OwnerId == customerId)
            return BookingOperationResult.Fail("Bạn không thể tự đặt xe của chính mình.");

        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        await _availability.LockVehiclesAsync(new[] { vehicle.Id });
        await _db.Entry(vehicle).ReloadAsync();
        if (!VehicleAvailabilityService.IsOperatingState(vehicle.TrangThai))
            return BookingOperationResult.Fail("Xe hiện không được phép cho thuê.");

        var conTrong = await KiemTraConTrongAsync(vehicle.Id, model.NgayBatDau, model.NgayKetThuc);
        if (!conTrong)
            return BookingOperationResult.Fail("Xe đã có người đặt trong khoảng ngày này. Vui lòng chọn khoảng ngày khác.");

        var booking = new Booking
        {
            VehicleId = vehicle.Id,
            CustomerId = customerId,
            NgayBatDau = model.NgayBatDau,
            NgayKetThuc = model.NgayKetThuc,
            GhiChu = model.GhiChu,
            GiaThueMotNgayLucDat = vehicle.GiaThueMotNgay,
            TongTien = TinhTongTien(vehicle.GiaThueMotNgay, model.NgayBatDau, model.NgayKetThuc),
            TrangThai = TrangThaiBooking.ChoXacNhan
        };

        booking = await _bookingRepository.AddAsync(booking);
        await transaction.CommitAsync();
        return BookingOperationResult.Ok(booking);
    }

    public async Task<BookingOperationResult> XacNhanAsync(Guid bookingId, Guid ownerId)
    {
        var booking = await _bookingRepository.GetByIdAsync(bookingId);
        if (booking is null) return BookingOperationResult.Fail("Không tìm thấy đơn đặt xe.");
        if (booking.Vehicle.OwnerId != ownerId) return BookingOperationResult.Fail("Bạn không có quyền xác nhận đơn này.");
        if (booking.TrangThai != TrangThaiBooking.ChoXacNhan)
            return BookingOperationResult.Fail("Đơn này không ở trạng thái chờ xác nhận.");

        // Recheck all order sources while holding the same vehicle lock as checkout.
        await using var confirmTx = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        await _availability.LockVehiclesAsync(new[] { booking.VehicleId });
        await _db.Entry(booking).ReloadAsync();
        if (booking.TrangThai != TrangThaiBooking.ChoXacNhan)
            return BookingOperationResult.Fail("Đơn không còn ở trạng thái chờ xác nhận.");
        var conTrong = await KiemTraConTrongAsync(booking.VehicleId, booking.NgayBatDau, booking.NgayKetThuc, booking.Id);
        if (!conTrong)
            return BookingOperationResult.Fail("Khoảng ngày này đã bị một đơn khác chiếm lịch trước. Vui lòng từ chối đơn này cho khách.");

        booking.TrangThai = TrangThaiBooking.DaXacNhan;
        booking.UpdatedAt = DateTime.UtcNow;
        await _bookingRepository.UpdateAsync(booking);
        await _contractService.TaoHopDongNeuChuaCoAsync(booking.Id);
        await confirmTx.CommitAsync();
        return BookingOperationResult.Ok(booking);
    }

    public async Task<BookingOperationResult> HuyAsync(Guid bookingId, Guid currentUserId, string? lyDo)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        var booking = await _bookingRepository.GetByIdAsync(bookingId);
        if (booking is null) return BookingOperationResult.Fail("Không tìm thấy đơn đặt xe.");
        if (booking.CustomerId != currentUserId && booking.Vehicle.OwnerId != currentUserId)
            return BookingOperationResult.Fail("Bạn không có quyền huỷ đơn này.");
        await _availability.LockVehiclesAsync(new[] { booking.VehicleId });
        await _db.Entry(booking).ReloadAsync();
        if (booking.TrangThai is not (TrangThaiBooking.ChoXacNhan or TrangThaiBooking.DaXacNhan))
            return BookingOperationResult.Fail("Đơn ở trạng thái hiện tại không thể huỷ.");
        // Legacy Booking has no general refund workflow. Do not release an already-paid
        // reservation and strand the customer's successful payment.
        if (await _paymentRepository.GetTongDaThanhToanAsync(booking.Id) > 0)
            return BookingOperationResult.Fail("Đơn đã thanh toán; vui lòng xử lý hoàn tiền với quản trị viên trước khi huỷ.");
        booking.TrangThai = TrangThaiBooking.DaHuy;
        booking.LyDoHuy = string.IsNullOrWhiteSpace(lyDo) ? null : lyDo.Trim()[..Math.Min(lyDo.Trim().Length, 300)];
        booking.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await tx.CommitAsync();
        return BookingOperationResult.Ok(booking);
    }

    public async Task<BookingOperationResult> XacNhanGiaoXeAsync(Guid bookingId, Guid ownerId)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        var booking = await _bookingRepository.GetByIdAsync(bookingId);
        if (booking is null) return BookingOperationResult.Fail("Không tìm thấy đơn đặt xe.");
        if (booking.Vehicle.OwnerId != ownerId)
            return BookingOperationResult.Fail("Bạn không có quyền giao xe cho đơn này.");
        await _availability.LockVehiclesAsync(new[] { booking.VehicleId });
        await _db.Entry(booking).ReloadAsync();
        await _db.Entry(booking.Vehicle).ReloadAsync();
        var error = RentalLifecyclePolicy.ValidateLegacyHandover(booking, RentalLifecyclePolicy.VietnamNow());
        if (error is not null) return BookingOperationResult.Fail(error);
        if (booking.Vehicle.TrangThai != TrangThaiXe.SanSang)
            return BookingOperationResult.Fail("Xe chưa sẵn sàng bàn giao thực tế.");
        if (await ActiveRentalGuard.HasOtherActiveRentalAsync(_db, booking.VehicleId, excludeBookingId: booking.Id))
            return BookingOperationResult.Fail("Xe đang được bàn giao cho một đơn khác.");
        var paid = await _paymentRepository.GetTongDaThanhToanAsync(booking.Id);
        if (paid < booking.TongTien)
            return BookingOperationResult.Fail($"Khách hàng chưa thanh toán đủ (đã thanh toán {paid:N0} đ / {booking.TongTien:N0} đ).");
        booking.TrangThai = TrangThaiBooking.DangThue;
        booking.Vehicle.TrangThai = TrangThaiXe.DangThue;
        booking.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await tx.CommitAsync();
        return BookingOperationResult.Ok(booking);
    }

    public async Task<BookingOperationResult> XacNhanTraXeAsync(Guid bookingId, Guid ownerId)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        var booking = await _bookingRepository.GetByIdAsync(bookingId);
        if (booking is null) return BookingOperationResult.Fail("Không tìm thấy đơn đặt xe.");
        if (booking.Vehicle.OwnerId != ownerId)
            return BookingOperationResult.Fail("Bạn không có quyền nhận lại xe cho đơn này.");
        await _availability.LockVehiclesAsync(new[] { booking.VehicleId });
        await _db.Entry(booking).ReloadAsync();
        await _db.Entry(booking.Vehicle).ReloadAsync();
        if (booking.TrangThai != TrangThaiBooking.DangThue)
            return BookingOperationResult.Fail("Đơn phải ở trạng thái đang thuê mới có thể xác nhận trả xe.");
        booking.TrangThai = TrangThaiBooking.HoanThanh;
        booking.UpdatedAt = DateTime.UtcNow;
        var otherActive = await ActiveRentalGuard.HasOtherActiveRentalAsync(_db, booking.VehicleId,
            excludeBookingId: booking.Id);
        if (RentalLifecyclePolicy.CanRestoreVehicleAvailability(booking.Vehicle.TrangThai, otherActive))
            booking.Vehicle.TrangThai = TrangThaiXe.SanSang;
        await _db.SaveChangesAsync();
        await tx.CommitAsync();
        return BookingOperationResult.Ok(booking);
    }

    public async Task<Booking?> GetDetailAsync(Guid id, Guid currentUserId, bool isAdmin)
    {
        var booking = await _bookingRepository.GetByIdAsync(id);
        if (booking is null) return null;
        if (!isAdmin && booking.CustomerId != currentUserId && booking.Vehicle.OwnerId != currentUserId)
            return null;
        return booking;
    }

    public Task<List<Booking>> GetMyBookingsAsync(Guid customerId) => _bookingRepository.GetByCustomerAsync(customerId);

    public Task<List<Booking>> GetOwnerRequestsAsync(Guid ownerId) => _bookingRepository.GetByOwnerVehiclesAsync(ownerId);
}