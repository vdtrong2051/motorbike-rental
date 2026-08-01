using RentalPlatform.Models.Entities;
using RentalPlatform.Models.ViewModels.Account;

namespace RentalPlatform.Services;

public interface IAccountService
{
    Task<(bool Success, string? Error)> RegisterAsync(RegisterViewModel model);
    Task<User?> ValidateLoginAsync(string email, string password);
}
