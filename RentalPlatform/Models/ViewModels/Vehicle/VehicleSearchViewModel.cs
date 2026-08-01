using RentalPlatform.Models.Entities;

namespace RentalPlatform.Models.ViewModels.Vehicle;

public class VehicleSearchViewModel
{
    public string? KhuVuc { get; set; }

    public string? LoaiXe { get; set; }

    public decimal? GiaTu { get; set; }

    public decimal? GiaDen { get; set; }

    // Chỉ hiển thị xe TrangThai = SanSang cho khách tìm kiếm công khai.
    // Owner/Admin xem danh sách riêng (MyVehicles / PendingVehicles) nên không dùng field này.
    public bool ChiXeSanSang { get; set; } = true;

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 9;

    // --- Dữ liệu trả về kèm theo để View render lại form + phân trang, không phải input của người dùng ---
    public int TotalCount { get; set; }

    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}

// Gói chung filter (để View render lại form tìm kiếm) và danh sách xe kết quả
public class VehicleSearchResultViewModel
{
    public VehicleSearchViewModel Filter { get; set; } = new();
    public List<Entities.Vehicle> Items { get; set; } = new();
}
