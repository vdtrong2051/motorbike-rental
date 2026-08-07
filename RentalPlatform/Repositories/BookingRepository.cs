using Microsoft.EntityFrameworkCore;
using RentalPlatform.Data;
using RentalPlatform.Models.Entities;

namespace RentalPlatform.Repositories;

public class BookingRepository : IBookingRepository
{
    private readonly AppDbContext _db;

    public BookingRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Booking?> GetByIdAsync(Guid id)
    {
        return await _db.Bookings
            .Include(b => b.Vehicle)
            .ThenInclude(v => v.HinhAnh)
            .Include(b => b.Customer)
            .Include(b => b.Contract)
            .Include(b => b.Review)
            .FirstOrDefaultAsync(b => b.Id == id);
    }

    public async Task<Booking> AddAsync(Booking booking)
    {
        _db.Bookings.Add(booking);
        await _db.SaveChangesAsync();
        return booking;
    }

    public async Task UpdateAsync(Booking booking)
    {
        _db.Bookings.Update(booking);
        await _db.SaveChangesAsync();
    }

    public async Task<List<Booking>> GetOverlappingAsync(Guid vehicleId, DateOnly tuNgay, DateOnly denNgay, Guid? loaiTruBookingId = null)
    {
        // Hai khoảng ngày [A,B] và [C,D] giao nhau khi A <= D và C <= B
        return await _db.Bookings
            .Where(b => b.VehicleId == vehicleId
                && (b.TrangThai == TrangThaiBooking.DaXacNhan || b.TrangThai == TrangThaiBooking.DangThue)
                && (loaiTruBookingId == null || b.Id != loaiTruBookingId)
                && b.NgayBatDau <= denNgay
                && tuNgay <= b.NgayKetThuc)
            .ToListAsync();
    }

    public async Task<List<Booking>> GetByCustomerAsync(Guid customerId)
    {
        return await _db.Bookings
            .Include(b => b.Vehicle)
            .ThenInclude(v => v.HinhAnh)
            .Include(b => b.Review)
            .Where(b => b.CustomerId == customerId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Booking>> GetByOwnerVehiclesAsync(Guid ownerId)
    {
        return await _db.Bookings
            .Include(b => b.Vehicle)
            .Include(b => b.Customer)
            .Where(b => b.Vehicle.OwnerId == ownerId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
    }
}