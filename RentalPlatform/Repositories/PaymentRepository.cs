using Microsoft.EntityFrameworkCore;
using RentalPlatform.Data;
using RentalPlatform.Models.Entities;

namespace RentalPlatform.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly AppDbContext _db;

    public PaymentRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Payment> AddAsync(Payment payment)
    {
        _db.Payments.Add(payment);
        await _db.SaveChangesAsync();
        return payment;
    }

    public async Task<List<Payment>> GetByBookingIdAsync(Guid bookingId)
    {
        return await _db.Payments
            .Where(p => p.BookingId == bookingId)
            .OrderBy(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<decimal> GetTongDaThanhToanAsync(Guid bookingId)
    {
        return await _db.Payments
            .Where(p => p.BookingId == bookingId && p.TrangThai == TrangThaiThanhToan.ThanhCong)
            .SumAsync(p => p.SoTien);
    }

    public async Task<bool> DaCoLoaiThanhToanAsync(Guid bookingId, LoaiThanhToan loai)
    {
        return await _db.Payments.AnyAsync(p =>
            p.BookingId == bookingId && p.LoaiThanhToan == loai && p.TrangThai == TrangThaiThanhToan.ThanhCong);
    }
}
