using Microsoft.EntityFrameworkCore;
using RentalPlatform.Data;
using RentalPlatform.Models.Entities;
using RentalPlatform.Models.ViewModels.Vehicle;

namespace RentalPlatform.Repositories;

public class VehicleRepository : IVehicleRepository
{
    private readonly AppDbContext _db;

    public VehicleRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Vehicle?> GetByIdAsync(Guid id, bool includeImages = true)
    {
        var query = _db.Vehicles.Include(v => v.Owner).AsQueryable();
        if (includeImages)
            query = query.Include(v => v.HinhAnh);

        return await query.FirstOrDefaultAsync(v => v.Id == id);
    }

    public async Task<Vehicle> AddAsync(Vehicle vehicle)
    {
        _db.Vehicles.Add(vehicle);
        await _db.SaveChangesAsync();
        return vehicle;
    }

    public async Task UpdateAsync(Vehicle vehicle, IReadOnlyCollection<VehicleImage>? newImages = null)
    {
        // Không gọi _db.Vehicles.Update(vehicle) ở đây: vehicle đã được EF Core
        // tracking sẵn (vừa Add hoặc vừa GetByIdAsync trong cùng request), nên
        // SaveChangesAsync() tự phát hiện đúng thay đổi. Gọi Update() sẽ duyệt lại
        // toàn bộ navigation graph và đánh dấu nhầm cả VehicleImage MỚI (vừa Add
        // vào vehicle.HinhAnh) thành "Modified" thay vì "Added" — vì Guid Id của
        // ảnh đã được sinh sẵn ở phía C# nên EF tưởng dòng đó đã tồn tại trong DB,
        // dẫn đến DbUpdateConcurrencyException (UPDATE khớp 0 dòng).
        // Đánh dấu tường minh các ảnh mới là Added để MySQL luôn nhận INSERT,
        // không phụ thuộc vào cách EF suy luận trạng thái từ Guid đã được sinh sẵn.
        if (newImages is { Count: > 0 })
            _db.VehicleImages.AddRange(newImages);

        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Vehicle vehicle)
    {
        _db.Vehicles.Remove(vehicle);
        await _db.SaveChangesAsync();
    }

    public async Task<bool> BienSoExistsAsync(string bienSo, Guid? excludeId = null)
    {
        return await _db.Vehicles.AnyAsync(v =>
            v.BienSo == bienSo && (excludeId == null || v.Id != excludeId));
    }

    public async Task<List<Vehicle>> GetByOwnerAsync(Guid ownerId)
    {
        return await _db.Vehicles
            .Include(v => v.HinhAnh)
            .Where(v => v.OwnerId == ownerId)
            .OrderByDescending(v => v.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Vehicle>> GetPendingApprovalAsync()
    {
        return await _db.Vehicles
            .Include(v => v.Owner)
            .Include(v => v.HinhAnh)
            .Where(v => v.TrangThai == TrangThaiXe.ChoDuyet)
            .OrderBy(v => v.CreatedAt)
            .ToListAsync();
    }

    public async Task<(List<Vehicle> Items, int TotalCount)> SearchAsync(VehicleSearchViewModel filter)
    {
        var query = _db.Vehicles.Include(v => v.HinhAnh).AsQueryable();

        if (filter.ChiXeSanSang)
            query = query.Where(v => v.TrangThai == TrangThaiXe.SanSang);

        if (!string.IsNullOrWhiteSpace(filter.TuKhoa))
        {
            var keyword = filter.TuKhoa.Trim();
            query = query.Where(v => v.TenXe.Contains(keyword)
                || v.BienSo.Contains(keyword)
                || v.LoaiXe.Contains(keyword)
                || (v.MoTa != null && v.MoTa.Contains(keyword)));
        }

        if (!string.IsNullOrWhiteSpace(filter.KhuVuc))
            query = query.Where(v => v.KhuVuc.Contains(filter.KhuVuc));

        if (!string.IsNullOrWhiteSpace(filter.LoaiXe))
            query = query.Where(v => v.LoaiXe.Contains(filter.LoaiXe));

        if (filter.GiaTu.HasValue)
            query = query.Where(v => v.GiaThueMotNgay >= filter.GiaTu.Value);

        if (filter.GiaDen.HasValue)
            query = query.Where(v => v.GiaThueMotNgay <= filter.GiaDen.Value);

        if (filter.HopSo.HasValue)
            query = query.Where(v => v.HopSo == filter.HopSo.Value);

        if (filter.NhienLieu.HasValue)
            query = query.Where(v => v.NhienLieu == filter.NhienLieu.Value);

        if (filter.SoChoNgoiToiThieu.HasValue)
            query = query.Where(v => v.SoChoNgoi >= filter.SoChoNgoiToiThieu.Value);

        if (filter.NgayNhanXe.HasValue && filter.NgayTraXe.HasValue && filter.NgayTraXe > filter.NgayNhanXe)
        {
            var start = filter.NgayNhanXe.Value;
            var end = filter.NgayTraXe.Value;
            var startDate = DateOnly.FromDateTime(start);
            var endDate = DateOnly.FromDateTime(end);

            query = query.Where(v =>
                !_db.Bookings.Any(b => b.VehicleId == v.Id
                    && b.TrangThai != TrangThaiBooking.DaHuy
                    && b.NgayBatDau <= endDate && b.NgayKetThuc >= startDate)
                && !_db.RentalOrderItems.Any(i => i.VehicleId == v.Id
                    && i.RentalOrder.TrangThai != TrangThaiDonThue.DaHuy
                    && i.NgayNhanXe < end && i.NgayTraXe > start));
        }

        var totalCount = await query.CountAsync();

        var page = Math.Max(filter.Page, 1);
        var pageSize = filter.PageSize <= 0 ? 9 : filter.PageSize;

        query = filter.SapXep switch
        {
            "gia-tang" => query.OrderBy(v => v.GiaThueMotNgay),
            "gia-giam" => query.OrderByDescending(v => v.GiaThueMotNgay),
            "nam-moi" => query.OrderByDescending(v => v.Nam),
            "ten" => query.OrderBy(v => v.TenXe),
            _ => query.OrderByDescending(v => v.CreatedAt)
        };

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }
}
