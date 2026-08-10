using RentalPlatform.Models.Entities;
using RentalPlatform.Models.ViewModels.Account;

namespace RentalPlatform.Services;

public interface IAccountService
{
    Task<(bool Success, string? Error)> RegisterAsync(RegisterViewModel model);
    Task<User?> ValidateLoginAsync(string email, string password);
    Task<ProfileViewModel?> GetProfileAsync(Guid userId);
    Task<(bool Success, string? Error)> UpdateProfileAsync(Guid userId, ProfileViewModel model);
    Task<(bool Success, string? Error)> SubmitVerificationAsync(Guid userId);
    Task<User> FindOrCreateExternalUserAsync(string provider, string subject, string email, string name);
    Task<IdentityDocumentContent?> GetIdentityDocumentAsync(Guid userId, string side);
}
