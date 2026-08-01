using RentalPlatform.Models.Entities;
using RentalPlatform.Models.ViewModels.Booking;
using RentalPlatform.Repositories;

namespace RentalPlatform.Services;

public class BookingService : IBookingService
{
    private readonly IBookingRepository _bookingRepository;
    private readonly IVehicleRepository _vehicleRepository;
    private readonly IContractService _contractService;
    private readonly IPaymentRepository _paymentRepository;

    public BookingService(
        IBookingRepository bookingRepository,
        IVehicleRepository vehicleRepository,
        IContractService contractService,
        IPaymentRepository paymentRepository)
    {
        _bookingRepository = bookingRepository;
        _vehicleRepository = vehicleRepository;
        _contractService = contractService;
        _paymentRepository = paymentRepository;
    }

    public async Task<bool> KiemTraConTrongAsync(Guid vehicleId, DateOnly tuNgay, DateOnly denNgay, Guid? loaiTruBookingId = null)
    {
        var trung = await _bookingRepository.GetOverlappingAsync(vehicleId, tuNgay, denNgay, loaiTruBookingId);
        return trung.Count == 0;
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

        if (model.NgayBatDau < DateOnly.FromDateTime(DateTime.Today))
            return BookingOperationResult.Fail("Ngày nhận xe không được ở trong quá khứ.");

        var vehicle = await _vehicleRepository.GetByIdAsync(model.VehicleId, includeImages: false);
        if (vehicle is null) return BookingOperationResult.Fail("Không tìm thấy xe.");

        if (vehicle.OwnerId == customerId)
            return BookingOperationResult.Fail("Bạn không thể tự đặt xe của chính mình.");

        if (vehicle.TrangThai != TrangThaiXe.SanSang)
            return BookingOperationResult.Fail("Xe hiện không sẵn sàng cho thuê.");

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
        return BookingOperationResult.Ok(booking);
    }

    public async Task<BookingOperationResult> XacNhanAsync(Guid bookingId, Guid ownerId)
    {
        var booking = await _bookingRepository.GetByIdAsync(bookingId);
        if (booking is null) return BookingOperationResult.Fail("Không tìm thấy đơn đặt xe.");
        if (booking.Vehicle.OwnerId != ownerId) return BookingOperationResult.Fail("Bạn không có quyền xác nhận đơn này.");
        if (booking.TrangThai != TrangThaiBooking.ChoXacNhan)
            return BookingOperationResult.Fail("Đơn này không ở trạng thái chờ xác nhận.");

        // Kiểm tra lại trùng lịch tại thời điểm xác nhận: có thể trong lúc đơn này còn
        // ChoXacNhan, một đơn khác của cùng xe (khoảng ngày giao nhau) đã được xác nhận trước.
        var conTrong = await KiemTraConTrongAsync(booking.VehicleId, booking.NgayBatDau, booking.NgayKetThuc, booking.Id);
        if (!conTrong)
            return BookingOperationResult.Fail("Khoảng ngày này đã bị một đơn khác chiếm lịch trước. Vui lòng từ chối đơn này cho khách.");

        booking.TrangThai = TrangThaiBooking.DaXacNhan;
        booking.UpdatedAt = DateTime.UtcNow;
        await _bookingRepository.UpdateAsync(booking);

        await _contractService.TaoHopDongNeuChuaCoAsync(booking.Id);

        return BookingOperationResult.Ok(booking);
    }

    public async Task<BookingOperationResult> HuyAsync(Guid bookingId, Guid currentUserId, string? lyDo)
    {
        var booking = await _bookingRepository.GetByIdAsync(bookingId);
        if (booking is null) return BookingOperationResult.Fail("Không tìm thấy đơn đặt xe.");

        var laKhach = booking.CustomerId == currentUserId;
        var laChuXe = booking.Vehicle.OwnerId == currentUserId;
        if (!laKhach && !laChuXe) return BookingOperationResult.Fail("Bạn không có quyền huỷ đơn này.");

        if (booking.TrangThai is not (TrangThaiBooking.ChoXacNhan or TrangThaiBooking.DaXacNhan))
            return BookingOperationResult.Fail("Đơn ở trạng thái hiện tại không thể huỷ.");

        booking.TrangThai = TrangThaiBooking.DaHuy;
        booking.LyDoHuy = lyDo;
        booking.UpdatedAt = DateTime.UtcNow;
        await _bookingRepository.UpdateAsync(booking);
        return BookingOperationResult.Ok(booking);
    }

    public async Task<BookingOperationResult> XacNhanGiaoXeAsync(Guid bookingId, Guid ownerId)
    {
        var booking = await _bookingRepository.GetByIdAsync(bookingId);
        if (booking is null) return BookingOperationResult.Fail("Không tìm thấy đơn đặt xe.");
        if (booking.Vehicle.OwnerId != ownerId) return BookingOperationResult.Fail("Bạn không có quyền giao xe cho đơn này.");
        if (booking.TrangThai != TrangThaiBooking.DaXacNhan)
            return BookingOperationResult.Fail("Đơn phải ở trạng thái đã xác nhận mới có thể giao xe.");

        var contract = booking.Contract;
        if (contract is null || !contract.DaKyKhach || !contract.DaKyChuXe)
            return BookingOperationResult.Fail("Cả hai bên cần ký hợp đồng điện tử trước khi giao xe.");

        var daThanhToan = await _paymentRepository.GetTongDaThanhToanAsync(booking.Id);
        if (daThanhToan < booking.TongTien)
            return BookingOperationResult.Fail($"Khách hàng chưa thanh toán đủ (đã thanh toán {daThanhToan:N0} đ / {booking.TongTien:N0} đ).");

        booking.TrangThai = TrangThaiBooking.DangThue;
        booking.UpdatedAt = DateTime.UtcNow;
        await _bookingRepository.UpdateAsync(booking);

        booking.Vehicle.TrangThai = TrangThaiXe.DangThue;
        await _vehicleRepository.UpdateAsync(booking.Vehicle);

        return BookingOperationResult.Ok(booking);
    }

    public async Task<BookingOperationResult> XacNhanTraXeAsync(Guid bookingId, Guid ownerId)
    {
        var booking = await _bookingRepository.GetByIdAsync(bookingId);
        if (booking is null) return BookingOperationResult.Fail("Không tìm thấy đơn đặt xe.");
        if (booking.Vehicle.OwnerId != ownerId) return BookingOperationResult.Fail("Bạn không có quyền nhận lại xe cho đơn này.");
        if (booking.TrangThai != TrangThaiBooking.DangThue)
            return BookingOperationResult.Fail("Đơn phải ở trạng thái đang thuê mới có thể xác nhận trả xe.");

        booking.TrangThai = TrangThaiBooking.HoanThanh;
        booking.UpdatedAt = DateTime.UtcNow;
        await _bookingRepository.UpdateAsync(booking);

        booking.Vehicle.TrangThai = TrangThaiXe.SanSang;
        await _vehicleRepository.UpdateAsync(booking.Vehicle);

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