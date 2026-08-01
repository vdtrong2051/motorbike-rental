using RentalPlatform.Models.Entities;
using RentalPlatform.Models.ViewModels.Payment;

namespace RentalPlatform.Services;

public class PaymentOperationResult
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public Payment? Payment { get; init; }

    public static PaymentOperationResult Ok(Payment payment) => new() { Success = true, Payment = payment };
    public static PaymentOperationResult Fail(string error) => new() { Success = false, Error = error };
}

public interface IPaymentService
{
    // Tỉ lệ đặt cọc mặc định, dùng để tính số tiền đặt cọc từ TongTien của Booking
    decimal TyLeDatCoc { get; }

    Task<PaymentOperationResult> DatCocAsync(PaymentCreateViewModel model, Guid currentUserId);
    Task<PaymentOperationResult> ThanhToanConLaiAsync(PaymentCreateViewModel model, Guid currentUserId);
    Task<List<Payment>> GetByBookingIdAsync(Guid bookingId);
    Task<decimal> GetTongDaThanhToanAsync(Guid bookingId);
}
