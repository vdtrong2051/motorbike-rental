using RentalPlatform.Models.ViewModels.Admin;

namespace RentalPlatform.Services;

public interface IDashboardService
{
    Task<AdminDashboardViewModel> GetAdminDashboardAsync();
}
