using Microsoft.EntityFrameworkCore;
using RentalPlatform.Data;
using RentalPlatform.Models.Entities;

namespace RentalPlatform.Repositories;

public class ReviewRepository : IReviewRepository
{
    private readonly AppDbContext _db;

    public ReviewRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Review?> GetByBookingIdAsync(Guid bookingId)
    {
        return await _db.Reviews.FirstOrDefaultAsync(r => r.BookingId == bookingId);
    }

    public async Task<Review> AddAsync(Review review)
    {
        _db.Reviews.Add(review);
        await _db.SaveChangesAsync();
        return review;
    }

    public async Task<List<Review>> GetByVehicleAsync(Guid vehicleId)
    {
        return await _db.Reviews
            .Include(r => r.Customer)
            .Where(r => r.VehicleId == vehicleId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<(double TrungBinh, int SoLuong)> GetThongKeAsync(Guid vehicleId)
    {
        var danhGia = await _db.Reviews
            .Where(r => r.VehicleId == vehicleId)
            .Select(r => r.SoSao)
            .ToListAsync();

        if (danhGia.Count == 0) return (0, 0);
        return (Math.Round(danhGia.Average(), 2), danhGia.Count);
    }
}
