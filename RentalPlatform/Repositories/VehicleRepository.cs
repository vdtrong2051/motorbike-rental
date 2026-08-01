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

    public async Task UpdateAsync(Vehicle vehicle)
    {
        // Không gọi _db.Vehicles.Update(vehicle) ở đây: vehicle đã được EF Core
        // tracking sẵn (vừa Add hoặc vừa GetByIdAsync trong cùng request), nên
        // SaveChangesAsync() tự phát hiện đúng thay đổi. Gọi Update() sẽ duyệt lại
        // toàn bộ navigation graph và đánh dấu nhầm cả VehicleImage MỚI (vừa Add
        // vào vehicle.HinhAnh) thành "Modified" thay vì "Added" — vì Guid Id của
        // ảnh đã được sinh sẵn ở phía C# nên EF tưởng dòng đó đã tồn tại trong DB,
        // dẫn đến DbUpdateConcurrencyException (UPDATE khớp 0 dòng).
        await _db.SaveChangesAsync();
    }

    public async Task AddImagesAsync(List<VehicleImage> images)
    {
        // KHÔNG thêm ảnh vào vehicle.HinhAnh (navigation của 1 entity đã tracking) rồi
        // gọi SaveChangesAsync() suông: EF chỉ tự nhận diện "Added" cho entity mới phát
        // hiện qua navigation nếu khoá chính đang ở giá trị mặc định (Guid.Empty). Vì
        // VehicleImage.Id được gán Guid.NewGuid() ngay lúc khởi tạo (không phải mặc
        // định), EF tưởng dòng đó đã tồn tại trong DB và sinh UPDATE thay vì INSERT
        // -> khớp 0 dòng -> DbUpdateConcurrencyException. AddRange() tường minh ở đây
        // buộc EF đánh dấu đúng trạng thái Added, bất kể Id đã có giá trị hay chưa.
        _db.VehicleImages.AddRange(images);
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

        if (!string.IsNullOrWhiteSpace(filter.KhuVuc))
            query = query.Where(v => v.KhuVuc.Contains(filter.KhuVuc));

        if (!string.IsNullOrWhiteSpace(filter.LoaiXe))
            query = query.Where(v => v.LoaiXe.Contains(filter.LoaiXe));

        if (filter.GiaTu.HasValue)
            query = query.Where(v => v.GiaThueMotNgay >= filter.GiaTu.Value);

        if (filter.GiaDen.HasValue)
            query = query.Where(v => v.GiaThueMotNgay <= filter.GiaDen.Value);

        var totalCount = await query.CountAsync();

        var page = Math.Max(filter.Page, 1);
        var pageSize = filter.PageSize <= 0 ? 9 : filter.PageSize;

        var items = await query
            .OrderByDescending(v => v.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }
}
