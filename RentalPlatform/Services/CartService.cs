using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RentalPlatform.Data;
using RentalPlatform.Models.Entities;
using RentalPlatform.Models.ViewModels.RentalOrder;
using RentalPlatform.Services.Availability;
using RentalPlatform.Services.Lifecycle;

namespace RentalPlatform.Services;

public record CartOperationResult(bool Success, string? Error = null);

public interface ICartService
{
    Task<CartViewModel> GetAsync();
    Task<CartOperationResult> AddAsync(Guid vehicleId, DateTime? ngayNhanXe = null, DateTime? ngayTraXe = null);
    Task<CartOperationResult> UpdateDatesAsync(Guid vehicleId, DateTime ngayNhanXe, DateTime ngayTraXe);
    Task RemoveAsync(Guid vehicleId);
    void Clear();
    int Count();
}

public class CartService : ICartService
{
    private const string SessionKey = "rental-cart";
    private const string SessionDatesKey = "rental-cart-dates";
    private record CartPeriod(DateTime Start, DateTime End);
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly AppDbContext _db;
    private readonly IVehicleAvailabilityService _availability;

    public CartService(IHttpContextAccessor httpContextAccessor, AppDbContext db,
        IVehicleAvailabilityService availability)
    {
        _httpContextAccessor = httpContextAccessor;
        _db = db;
        _availability = availability;
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

    private Dictionary<Guid, CartPeriod> GetDates()
    {
        var raw = Session.GetString(SessionDatesKey);
        return string.IsNullOrWhiteSpace(raw)
            ? new Dictionary<Guid, CartPeriod>()
            : JsonSerializer.Deserialize<Dictionary<Guid, CartPeriod>>(raw) ?? new();
    }
    private void SaveDates(Dictionary<Guid, CartPeriod> dates) =>
        Session.SetString(SessionDatesKey, JsonSerializer.Serialize(dates));

    private static bool ValidPeriod(DateTime start, DateTime end) =>
        start >= RentalLifecyclePolicy.VietnamNow().AddMinutes(-5) && end > start && (end - start).TotalDays <= 365;

    public async Task<CartViewModel> GetAsync()
    {
        var ids = GetIds();
        if (ids.Count == 0) return new CartViewModel();

        var selectedDates = GetDates();
        var defaultStart = RentalLifecyclePolicy.VietnamNow().Date.AddDays(1).AddHours(8);
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
                NgayNhanXe = selectedDates.TryGetValue(v.Id, out var dates1) ? dates1.Start : defaultStart,
                NgayTraXe = selectedDates.TryGetValue(v.Id, out var dates2) ? dates2.End : defaultStart.AddDays(1),
                AnhDaiDien = (v.HinhAnh.FirstOrDefault(i => i.LaAnhDaiDien) ?? v.HinhAnh.FirstOrDefault())?.DuongDanAnh
            })
            .ToList();

        if (items.Count != ids.Count) SaveIds(items.Select(i => i.VehicleId).ToList());
        return new CartViewModel { Items = items };
    }

    public async Task<CartOperationResult> AddAsync(Guid vehicleId, DateTime? ngayNhanXe = null, DateTime? ngayTraXe = null)
    {
        var vehicle = await _db.Vehicles.AsNoTracking().FirstOrDefaultAsync(v => v.Id == vehicleId);
        if (vehicle is null) return new(false, "Không tìm thấy xe.");
        if (!VehicleAvailabilityService.IsOperatingState(vehicle.TrangThai))
            return new(false, "Xe hiện không được phép cho thuê.");
        if (ngayNhanXe.HasValue != ngayTraXe.HasValue)
            return new(false, "Cần cung cấp cả ngày nhận và ngày trả xe.");
        if (ngayNhanXe.HasValue)
        {
            if (!ValidPeriod(ngayNhanXe.Value, ngayTraXe!.Value))
                return new(false, "Khoảng thời gian thuê không hợp lệ.");
            if (await _availability.IsBusyAsync(vehicleId, ngayNhanXe.Value, ngayTraXe.Value))
                return new(false, "Xe đã có lịch thuê trùng khoảng thời gian đã chọn.");
        }

        var ids = GetIds();
        if (ids.Contains(vehicleId))
        {
            if (ngayNhanXe.HasValue)
            {
                var existingDates = GetDates();
                existingDates[vehicleId] = new CartPeriod(ngayNhanXe.Value, ngayTraXe!.Value);
                SaveDates(existingDates);
            }
            return new(true);
        }

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
        if (ngayNhanXe.HasValue)
        {
            var selectedDates = GetDates();
            selectedDates[vehicleId] = new CartPeriod(ngayNhanXe.Value, ngayTraXe!.Value);
            SaveDates(selectedDates);
        }
        return new(true);
    }

    public async Task<CartOperationResult> UpdateDatesAsync(Guid vehicleId, DateTime ngayNhanXe, DateTime ngayTraXe)
    {
        if (!GetIds().Contains(vehicleId)) return new(false, "Xe không còn trong giỏ hàng.");
        if (!ValidPeriod(ngayNhanXe, ngayTraXe)) return new(false, "Khoảng thời gian thuê không hợp lệ.");
        if (!await _availability.IsBookableAsync(vehicleId, ngayNhanXe, ngayTraXe))
            return new(false, "Xe không khả dụng trong khoảng thời gian đã chọn.");
        var dates = GetDates();
        dates[vehicleId] = new CartPeriod(ngayNhanXe, ngayTraXe);
        SaveDates(dates);
        return new(true);
    }

    public Task RemoveAsync(Guid vehicleId)
    {
        var ids = GetIds();
        ids.RemoveAll(id => id == vehicleId);
        SaveIds(ids);
        var dates = GetDates();
        dates.Remove(vehicleId);
        SaveDates(dates);
        return Task.CompletedTask;
    }

    public void Clear()
    {
        Session.Remove(SessionKey);
        Session.Remove(SessionDatesKey);
    }

    public int Count() => GetIds().Count;
}
