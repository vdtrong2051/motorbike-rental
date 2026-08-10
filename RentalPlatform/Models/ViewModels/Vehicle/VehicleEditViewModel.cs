using System.ComponentModel.DataAnnotations;
using RentalPlatform.Models.Entities;

namespace RentalPlatform.Models.ViewModels.Vehicle;

public class VehicleEditViewModel
{
    [Required]
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên xe")]
    [Display(Name = "Tên xe")]
    public string TenXe { get; set; } = string.Empty;

    [Required, MaxLength(100), Display(Name = "Hãng xe")]
    public string HangXe { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập loại xe")]
    [MaxLength(50, ErrorMessage = "Loại xe tối đa 50 ký tự")]
    [Display(Name = "Loại xe")]
    public string LoaiXe { get; set; } = string.Empty;

    [Range(1, 16, ErrorMessage = "Số chỗ ngồi phải từ 1 đến 16")]
    [Display(Name = "Số chỗ ngồi")]
    public int SoChoNgoi { get; set; }

    [Required]
    [Display(Name = "Hộp số")]
    public HopSoXe HopSo { get; set; }

    [Required]
    [Display(Name = "Nhiên liệu")]
    public NhienLieuXe NhienLieu { get; set; }

    [Range(1990, 2100, ErrorMessage = "Năm sản xuất không hợp lệ")]
    [Display(Name = "Năm sản xuất")]
    public int Nam { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập khu vực")]
    [Display(Name = "Khu vực (Quận/Huyện, Tỉnh/Thành phố)")]
    public string KhuVuc { get; set; } = string.Empty;

    [Display(Name = "Địa chỉ cụ thể")]
    public string? DiaChiCuThe { get; set; }

    [Range(0, 100_000_000, ErrorMessage = "Giá thuê không hợp lệ")]
    [Display(Name = "Giá thuê / ngày (VNĐ)")]
    public decimal GiaThueMotNgay { get; set; }

    [Display(Name = "Mô tả")]
    public string? MoTa { get; set; }

    // Cho phép thêm ảnh mới khi sửa xe (bằng link); xoá ảnh cũ được xử lý qua action riêng (DeleteImage)
    [Display(Name = "Thêm ảnh mới (mỗi dòng 1 link, bắt đầu bằng http:// hoặc https://)")]
    public string? NewImageUrls { get; set; }

    // Chỉ để hiển thị lại trên form khi ModelState invalid, không phải input trực tiếp
    public string BienSo { get; set; } = string.Empty;
}
