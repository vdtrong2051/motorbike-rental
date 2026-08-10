using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RentalPlatform.Data;
using RentalPlatform.Models.Entities;
using RentalPlatform.Models.ViewModels.Account;

namespace RentalPlatform.Services;

public class AccountService : IAccountService
{
    private readonly AppDbContext _db;
    // PasswordHasher<User> đến từ Microsoft.AspNetCore.Identity, có sẵn trong shared framework,
    // không cần cài thêm package Identity đầy đủ.
    private readonly PasswordHasher<User> _hasher = new();

    public AccountService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<(bool Success, string? Error)> RegisterAsync(RegisterViewModel model)
    {
        var existed = await _db.Users.AnyAsync(u => u.Email == model.Email);
        if (existed)
            return (false, "Email này đã được đăng ký.");

        var user = new User
        {
            HoTen = model.HoTen,
            Email = model.Email,
            SDT = model.SDT,
            Role = model.IsOwner ? UserRole.Owner : UserRole.Customer
        };
        user.PasswordHash = _hasher.HashPassword(user, model.Password);

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return (true, null);
    }

    public async Task<User?> ValidateLoginAsync(string email, string password)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user is null) return null;

        var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, password);
        return result == PasswordVerificationResult.Failed ? null : user;
    }

    public async Task<ProfileViewModel?> GetProfileAsync(Guid userId)
    {
        return await _db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new ProfileViewModel
            {
                HoTen = u.HoTen,
                Email = u.Email,
                SDT = u.SDT,
                SoCCCD = u.SoCCCD,
                SoGPLX = u.SoGPLX,
                DiaChi = u.DiaChi,
                Role = u.Role.ToString()
            })
            .FirstOrDefaultAsync();
    }

    public async Task<(bool Success, string? Error)> UpdateProfileAsync(Guid userId, ProfileViewModel model)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null) return (false, "Không tìm thấy tài khoản.");

        user.HoTen = model.HoTen.Trim();
        user.SDT = string.IsNullOrWhiteSpace(model.SDT) ? null : model.SDT.Trim();
        user.SoCCCD = string.IsNullOrWhiteSpace(model.SoCCCD) ? null : model.SoCCCD.Trim();
        user.SoGPLX = string.IsNullOrWhiteSpace(model.SoGPLX) ? null : model.SoGPLX.Trim();
        user.DiaChi = string.IsNullOrWhiteSpace(model.DiaChi) ? null : model.DiaChi.Trim();
        await _db.SaveChangesAsync();
        return (true, null);
    }
}
