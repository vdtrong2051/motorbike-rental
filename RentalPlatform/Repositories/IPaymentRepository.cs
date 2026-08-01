using RentalPlatform.Models.Entities;

namespace RentalPlatform.Repositories;

public interface IPaymentRepository
{
    Task<Payment> AddAsync(Payment payment);
    Task<List<Payment>> GetByBookingIdAsync(Guid bookingId);
    Task<decimal> GetTongDaThanhToanAsync(Guid bookingId);
    Task<bool> DaCoLoaiThanhToanAsync(Guid bookingId, LoaiThanhToan loai);
}
