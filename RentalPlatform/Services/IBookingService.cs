using RentalPlatform.Models.Entities;
using RentalPlatform.Models.ViewModels.Booking;

namespace RentalPlatform.Services;

public class BookingOperationResult
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public Booking? Booking { get; init; }

    public static BookingOperationResult Ok(Booking booking) => new() { Success = true, Booking = booking };
    public static BookingOperationResult Fail(string error) => new() { Success = false, Error = error };
}

public interface IBookingService
{
    // Trả về true nếu xe còn trống trong khoảng [tuNgay, denNgay] — chỉ tính các booking
    // đang ở trạng thái DaXacNhan hoặc DangThue là chiếm lịch.
    Task<bool> KiemTraConTrongAsync(Guid vehicleId, DateOnly tuNgay, DateOnly denNgay, Guid? loaiTruBookingId = null);

    decimal TinhTongTien(decimal giaMotNgay, DateOnly ngayBatDau, DateOnly ngayKetThuc);

    Task<BookingOperationResult> CreateAsync(BookingCreateViewModel model, Guid customerId);

    // Owner xác nhận đơn đặt xe (ChoXacNhan -> DaXacNhan)
    Task<BookingOperationResult> XacNhanAsync(Guid bookingId, Guid ownerId);

    // Customer hoặc Owner huỷ đơn (ChoXacNhan/DaXacNhan -> DaHuy)
    Task<BookingOperationResult> HuyAsync(Guid bookingId, Guid currentUserId, string? lyDo);

    // Owner xác nhận giao xe cho khách (DaXacNhan -> DangThue), yêu cầu hợp đồng đã ký đủ
    // 2 bên và đã thanh toán đủ tổng tiền. Đồng thời cập nhật Vehicle.TrangThai -> DangThue.
    Task<BookingOperationResult> XacNhanGiaoXeAsync(Guid bookingId, Guid ownerId);

    // Owner xác nhận đã nhận lại xe từ khách (DangThue -> HoanThanh).
    // Đồng thời cập nhật Vehicle.TrangThai -> SanSang.
    Task<BookingOperationResult> XacNhanTraXeAsync(Guid bookingId, Guid ownerId);

    Task<Booking?> GetDetailAsync(Guid id, Guid currentUserId, bool isAdmin);
    Task<List<Booking>> GetMyBookingsAsync(Guid customerId);
    Task<List<Booking>> GetOwnerRequestsAsync(Guid ownerId);
}