using Microsoft.EntityFrameworkCore;
using RentalPlatform.Data;
using RentalPlatform.Models.Entities;

namespace RentalPlatform.Services.Lifecycle;

/// <summary>
/// GD5-C: order-level transition preconditions and Vietnam civil-time policy.
/// CreatedAt, UpdatedAt and payment timestamps continue to be stored in UTC.
/// NgayNhanXe / NgayTraXe remain pre-existing Vietnam local wall-clock values.
/// </summary>
public static class RentalLifecyclePolicy
{
    private static readonly TimeZoneInfo VietnamZone = ResolveVietnamZone();

    private static TimeZoneInfo ResolveVietnamZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"); }
    }

    public static DateTime VietnamFromUtc(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), VietnamZone);

    public static DateTime VietnamNow() => VietnamFromUtc(DateTime.UtcNow);

    public static bool CanSignContract(TrangThaiDonThue state) =>
        state is TrangThaiDonThue.DaXacNhan or TrangThaiDonThue.ChoThanhToan
            or TrangThaiDonThue.DaThanhToan;

    public static bool CanCancel(TrangThaiDonThue state) =>
        state is not (TrangThaiDonThue.DangThue or TrangThaiDonThue.HoanThanh
            or TrangThaiDonThue.DaHuy);

    public static string? ValidateHandover(RentalOrder order, DateTime localNow)
    {
        if (order.TrangThai != TrangThaiDonThue.DaThanhToan
            || order.TrangThaiThanhToan != TrangThaiThanhToanDon.DaThanhToan)
            return "Đơn phải ở trạng thái đã thanh toán trước khi giao xe.";
        if (order.Contract is null || !order.Contract.DaKyKhach || !order.Contract.DaKyChuXe)
            return "Hợp đồng phải được cả hai bên ký trước khi giao xe.";
        if (order.Items.Count == 0)
            return "Đơn thuê không có xe để bàn giao.";
        // One order has one handover action: ALL items must be physically due for pickup.
        // Do not mark vehicles from different future dates as 'DangThue' prematurely.
        if (order.Items.Any(i => i.NgayNhanXe > localNow.AddHours(2)))
            return "Chưa tới thời gian giao toàn bộ xe (chỉ được giao sớm tối đa 2 giờ).";
        if (order.Items.Any(i => i.NgayTraXe <= localNow))
            return "Đã quá thời gian trả theo hợp đồng; cần điều chỉnh lịch trước khi giao xe.";
        return null;
    }

    public static string? ValidateLegacyHandover(Booking booking, DateTime localNow)
    {
        if (booking.TrangThai != TrangThaiBooking.DaXacNhan)
            return "Đơn phải được xác nhận trước khi giao xe.";
        var today = DateOnly.FromDateTime(localNow);
        if (today < booking.NgayBatDau)
            return "Chưa đến ngày nhận xe của đơn đặt.";
        if (today > booking.NgayKetThuc)
            return "Đã quá ngày trả xe theo đơn đặt.";
        if (booking.Contract is null || !booking.Contract.DaKyKhach || !booking.Contract.DaKyChuXe)
            return "Cả hai bên cần ký hợp đồng điện tử trước khi giao xe.";
        return null;
    }

    public static bool CanRestoreVehicleAvailability(TrangThaiXe state, bool otherRentalActive) =>
        state == TrangThaiXe.DangThue && !otherRentalActive;

    public static bool FinancialBreakdownMatches(RentalOrder order) =>
        order.TienThue + order.TienDatCoc + order.PhiDichVu == order.TongTien;
}

/// <summary>
/// Detects actual, currently handed-over rentals across BOTH Booking and RentalOrder.
/// Must be called under the GD5-B vehicle FOR UPDATE lock for lifecycle writes.
/// Dates are intentionally not filtered: an overdue unreturned vehicle is still in use.
/// </summary>
public static class ActiveRentalGuard
{
    public static async Task<bool> HasOtherActiveRentalAsync(AppDbContext db, Guid vehicleId,
        Guid? excludeBookingId = null, Guid? excludeRentalOrderId = null)
    {
        if (await db.Bookings.AsNoTracking().AnyAsync(b => b.VehicleId == vehicleId
            && (excludeBookingId == null || b.Id != excludeBookingId)
            && b.TrangThai == TrangThaiBooking.DangThue))
            return true;
        return await db.RentalOrderItems.AsNoTracking().AnyAsync(i => i.VehicleId == vehicleId
            && (excludeRentalOrderId == null || i.RentalOrderId != excludeRentalOrderId)
            && i.RentalOrder.TrangThai == TrangThaiDonThue.DangThue);
    }
}
