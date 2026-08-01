using Microsoft.EntityFrameworkCore;
using RentalPlatform.Data;
using RentalPlatform.Models.Entities;

namespace RentalPlatform.Repositories;

public class ContractRepository : IContractRepository
{
    private readonly AppDbContext _db;

    public ContractRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Contract?> GetByBookingIdAsync(Guid bookingId)
    {
        return await _db.Contracts
            .Include(c => c.Booking)
            .ThenInclude(b => b.Vehicle)
            .Include(c => c.Booking)
            .ThenInclude(b => b.Customer)
            .FirstOrDefaultAsync(c => c.BookingId == bookingId);
    }

    public async Task<Contract> AddAsync(Contract contract)
    {
        _db.Contracts.Add(contract);
        await _db.SaveChangesAsync();
        return contract;
    }

    public async Task UpdateAsync(Contract contract)
    {
        _db.Contracts.Update(contract);
        await _db.SaveChangesAsync();
    }

    public async Task<bool> SoHopDongExistsAsync(string soHopDong)
    {
        return await _db.Contracts.AnyAsync(c => c.SoHopDong == soHopDong);
    }
}
