using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RentalPlatform.Data;
using RentalPlatform.Models.Entities;
using RentalPlatform.Models.ViewModels.RentalOrder;

namespace RentalPlatform.Services;

public record CartOperationResult(bool Success, string? Error = null);

public interface ICartService
{
    Task<CartViewModel> GetAsync();
    Task<CartOperationResult> AddAsync(Guid vehicleId);
    Task RemoveAsync(Guid vehicleId);
    void Clear();
    int Count();
}

public class CartService : ICartService
{
    private const string SessionKey = "rental-cart";
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly AppDbContext _db;

    public CartService(IHttpContextAccessor httpContextAccessor, AppDbContext db)
    {
        _httpContextAccessor = httpContextAccessor;
        _db = db;
    }

    private ISession Session => _httpContextAccessor.HttpContext?.Session
        ?? throw new InvalidOperationException("Không có phiên HTTP.");

    private List<Guid> GetIds()
    {
        var raw = Session.GetString(SessionKey);
        return string.IsNullOrWhiteSpace(raw)
            ? new List<Guid>()
            : JsonSerializer.Deserialize<List<Guid>>(raw) ?? new List<Guid>();
    }

    private void SaveIds(List<Guid> ids) =>
        Session.SetString(SessionKey, JsonSerializer.Serialize(ids.Distinct().ToList()));

    public async Task<CartViewModel> GetAsync()
    {
        var ids = GetIds();
        if (ids.Count == 0) return new CartViewModel();

        var vehicles = await _db.Vehicles
            .AsNoTracking()
            .Include(v => v.HinhAnh)
            .Where(v => ids.Contains(v.Id))
            .ToListAsync();

        var items = ids
            .Select(id => vehicles.FirstOrDefault(v => v.Id == id))
            .Where(v => v is not null)
            .Select(v => new CartItemViewModel
            {
                VehicleId = v!.Id,
                TenXe = v.TenXe,
                BienSo = v.BienSo,
                LoaiXe = v.LoaiXe,
                KhuVuc = v.KhuVuc,
                GiaThueMotNgay = v.GiaThueMotNgay,
                OwnerId = v.OwnerId,
                AnhDaiDien = (v.HinhAnh.FirstOrDefault(i => i.LaAnhDaiDien) ?? v.HinhAnh.FirstOrDefault())?.DuongDanAnh
            })
            .ToList();

        if (items.Count != ids.Count) SaveIds(items.Select(i => i.VehicleId).ToList());
        return new CartViewModel { Items = items };
    }

    public async Task<CartOperationResult> AddAsync(Guid vehicleId)
    {
        var vehicle = await _db.Vehicles.AsNoTracking().FirstOrDefaultAsync(v => v.Id == vehicleId);
        if (vehicle is null) return new(false, "Không tìm thấy xe.");
        if (vehicle.TrangThai != TrangThaiXe.SanSang)
            return new(false, "Xe hiện không sẵn sàng để thêm vào giỏ.");

        var ids = GetIds();
        if (ids.Contains(vehicleId)) return new(true);

        if (ids.Count > 0)
        {
            var ownerIds = await _db.Vehicles.AsNoTracking()
                .Where(v => ids.Contains(v.Id))
                .Select(v => v.OwnerId)
                .Distinct()
                .ToListAsync();
            if (ownerIds.Any(id => id != vehicle.OwnerId))
                return new(false, "Một đơn chỉ có thể chứa xe của cùng một chủ. Vui lòng hoàn tất hoặc làm trống giỏ hiện tại.");
        }

        ids.Add(vehicleId);
        SaveIds(ids);
        return new(true);
    }

    public Task RemoveAsync(Guid vehicleId)
    {
        var ids = GetIds();
        ids.RemoveAll(id => id == vehicleId);
        SaveIds(ids);
        return Task.CompletedTask;
    }

    public void Clear() => Session.Remove(SessionKey);

    public int Count() => GetIds().Count;
}
