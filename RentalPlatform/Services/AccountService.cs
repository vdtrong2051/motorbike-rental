using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RentalPlatform.Data;
using RentalPlatform.Models.Entities;
using RentalPlatform.Models.ViewModels.Account;

namespace RentalPlatform.Services;

public class AccountService : IAccountService
{
    private readonly AppDbContext _db;
    private readonly IIdentityDocumentStorage _identityDocuments;
    private readonly PasswordHasher<User> _hasher = new();

    public AccountService(AppDbContext db, IIdentityDocumentStorage identityDocuments)
    {
        _db = db;
        _identityDocuments = identityDocuments;
    }

    public async Task<(bool Success, string? Error)> RegisterAsync(RegisterViewModel model)
    {
        if (await _db.Users.AnyAsync(u => u.Email == model.Email))
            return (false, "Email này đã được đăng ký.");
        var user = new User
        {
            HoTen = model.HoTen, Email = model.Email, SDT = model.SDT,
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
        if (user is null || user.DangBiTamKhoa || string.IsNullOrWhiteSpace(user.PasswordHash)) return null;
        return _hasher.VerifyHashedPassword(user, user.PasswordHash, password) == PasswordVerificationResult.Failed
            ? null : user;
    }

    public async Task<ProfileViewModel?> GetProfileAsync(Guid userId)
    {
        return await _db.Users.AsNoTracking().Where(u => u.Id == userId)
            .Select(u => new ProfileViewModel
            {
                HoTen = u.HoTen, Email = u.Email, SDT = u.SDT, SoCCCD = u.SoCCCD,
                SoGPLX = u.SoGPLX, DiaChi = u.DiaChi, NgaySinh = u.NgaySinh, GioiTinh = u.GioiTinh,
                Role = u.Role.ToString(), TrangThaiXacThuc = u.TrangThaiXacThuc,
                GhiChuXacThuc = u.GhiChuXacThuc,
                CoAnhCCCDMatTruoc = u.AnhCCCDMatTruoc != null,
                CoAnhCCCDMatSau = u.AnhCCCDMatSau != null,
                HoSoDayDu = !string.IsNullOrEmpty(u.HoTen) && !string.IsNullOrEmpty(u.Email)
                    && !string.IsNullOrEmpty(u.SDT) && !string.IsNullOrEmpty(u.SoCCCD)
                    && !string.IsNullOrEmpty(u.AnhCCCDMatTruoc) && !string.IsNullOrEmpty(u.AnhCCCDMatSau)
                    && !string.IsNullOrEmpty(u.SoGPLX) && !string.IsNullOrEmpty(u.DiaChi)
                    && u.NgaySinh.HasValue && !string.IsNullOrEmpty(u.GioiTinh),
                DangBiTamKhoa = u.BiTamKhoa && (!u.TamKhoaDen.HasValue || u.TamKhoaDen > DateTime.UtcNow)
            }).FirstOrDefaultAsync();
    }

    public async Task<(bool Success, string? Error)> UpdateProfileAsync(Guid userId, ProfileViewModel model)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null) return (false, "Không tìm thấy tài khoản.");

        string? newFront = null;
        string? newBack = null;
        if (model.AnhCCCDMatTruocUpload is not null)
        {
            var saved = await _identityDocuments.SaveAsync(model.AnhCCCDMatTruocUpload, userId, "front");
            if (!saved.Success) return (false, saved.Error);
            newFront = saved.FileName;
        }
        if (model.AnhCCCDMatSauUpload is not null)
        {
            var saved = await _identityDocuments.SaveAsync(model.AnhCCCDMatSauUpload, userId, "back");
            if (!saved.Success)
            {
                await _identityDocuments.DeleteAsync(newFront);
                return (false, saved.Error);
            }
            newBack = saved.FileName;
        }

        var oldFront = user.AnhCCCDMatTruoc;
        var oldBack = user.AnhCCCDMatSau;
        if (newFront is not null) user.AnhCCCDMatTruoc = newFront;
        if (newBack is not null) user.AnhCCCDMatSau = newBack;
        user.HoTen = model.HoTen.Trim();
        user.SDT = model.SDT?.Trim(); user.SoCCCD = model.SoCCCD?.Trim(); user.SoGPLX = model.SoGPLX?.Trim();
        user.DiaChi = model.DiaChi?.Trim(); user.NgaySinh = model.NgaySinh; user.GioiTinh = model.GioiTinh?.Trim();
        user.TrangThaiXacThuc = TrangThaiXacThucNguoiDung.ChuaGui;
        user.GhiChuXacThuc = null; user.XacThucAt = null;
        try
        {
            await _db.SaveChangesAsync();
        }
        catch
        {
            await _identityDocuments.DeleteAsync(newFront);
            await _identityDocuments.DeleteAsync(newBack);
            throw;
        }
        if (newFront is not null) await _identityDocuments.DeleteAsync(oldFront);
        if (newBack is not null) await _identityDocuments.DeleteAsync(oldBack);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> SubmitVerificationAsync(Guid userId)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null) return (false, "Không tìm thấy tài khoản.");
        if (!user.HoSoDayDu) return (false, "Vui lòng điền đầy đủ hồ sơ và tải cả hai mặt CCCD trước khi gửi xác thực.");
        user.TrangThaiXacThuc = TrangThaiXacThucNguoiDung.ChoDuyet;
        user.GuiXacThucAt = DateTime.UtcNow; user.GhiChuXacThuc = null;
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<User> FindOrCreateExternalUserAsync(string provider, string subject, string email, string name)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u =>
            (u.OAuthProvider == provider && u.OAuthSubject == subject) || u.Email == email);
        if (user is null)
        {
            user = new User { Email = email, HoTen = name, OAuthProvider = provider, OAuthSubject = subject };
            _db.Users.Add(user);
        }
        else if (string.IsNullOrWhiteSpace(user.OAuthProvider))
        {
            user.OAuthProvider = provider; user.OAuthSubject = subject;
        }
        await _db.SaveChangesAsync();
        return user;
    }

    public async Task<IdentityDocumentContent?> GetIdentityDocumentAsync(Guid userId, string side)
    {
        var fileName = await _db.Users.AsNoTracking().Where(u => u.Id == userId)
            .Select(u => side == "back" ? u.AnhCCCDMatSau : u.AnhCCCDMatTruoc)
            .FirstOrDefaultAsync();
        return await _identityDocuments.ReadAsync(fileName);
    }
}
