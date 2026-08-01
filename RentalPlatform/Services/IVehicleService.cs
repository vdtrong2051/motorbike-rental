using RentalPlatform.Models.Entities;
using RentalPlatform.Models.ViewModels.Vehicle;

namespace RentalPlatform.Services;

public class VehicleOperationResult
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public Vehicle? Vehicle { get; init; }

    public static VehicleOperationResult Ok(Vehicle vehicle) => new() { Success = true, Vehicle = vehicle };
    public static VehicleOperationResult Fail(string error) => new() { Success = false, Error = error };
}

public interface IVehicleService
{
    Task<VehicleOperationResult> CreateAsync(VehicleCreateViewModel model, Guid ownerId);
    Task<VehicleOperationResult> UpdateAsync(VehicleEditViewModel model, Guid currentUserId);
    Task<VehicleOperationResult> DeleteAsync(Guid vehicleId, Guid currentUserId);
    Task<VehicleOperationResult> DeleteImageAsync(Guid vehicleId, Guid imageId, Guid currentUserId);
    Task<VehicleOperationResult> ChangeStatusByOwnerAsync(Guid vehicleId, Guid currentUserId, TrangThaiXe newStatus);
    Task<VehicleOperationResult> ApproveAsync(Guid vehicleId);
    Task<VehicleOperationResult> RejectAsync(Guid vehicleId, string lyDo);
    Task<Vehicle?> GetDetailAsync(Guid id);
    Task<List<Vehicle>> GetMyVehiclesAsync(Guid ownerId);
    Task<List<Vehicle>> GetPendingApprovalAsync();
    Task<VehicleSearchResultViewModel> SearchAsync(VehicleSearchViewModel filter);
}
