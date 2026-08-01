using RentalPlatform.Models.Entities;
using RentalPlatform.Models.ViewModels.Vehicle;

namespace RentalPlatform.Repositories;

public interface IVehicleRepository
{
    Task<Vehicle?> GetByIdAsync(Guid id, bool includeImages = true);
    Task<Vehicle> AddAsync(Vehicle vehicle);
    Task UpdateAsync(Vehicle vehicle);
    Task AddImagesAsync(List<VehicleImage> images);
    Task DeleteAsync(Vehicle vehicle);
    Task<bool> BienSoExistsAsync(string bienSo, Guid? excludeId = null);
    Task<List<Vehicle>> GetByOwnerAsync(Guid ownerId);
    Task<List<Vehicle>> GetPendingApprovalAsync();
    Task<(List<Vehicle> Items, int TotalCount)> SearchAsync(VehicleSearchViewModel filter);
}
