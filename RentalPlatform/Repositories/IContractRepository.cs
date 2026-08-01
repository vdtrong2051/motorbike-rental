using RentalPlatform.Models.Entities;

namespace RentalPlatform.Repositories;

public interface IContractRepository
{
    Task<Contract?> GetByBookingIdAsync(Guid bookingId);
    Task<Contract> AddAsync(Contract contract);
    Task UpdateAsync(Contract contract);
    Task<bool> SoHopDongExistsAsync(string soHopDong);
}
