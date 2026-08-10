using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RentalPlatform.Models.Entities;

public enum HopSoXe
{
    [Display(Name = "Số Sàn")]
    SoSan,

    [Display(Name = "Số Tự Động")]
    SoTuDong,

    [Display(Name = "Số vô cấp")]
    SoVoCap,

    [Display(Name = "Số có cấp")]
    SoCoCap
}

public enum NhienLieuXe
{
    [Display(Name = "Xăng")]
    Xang,

    [Display(Name = "Dầu")]
    Dau,

    [Display(Name = "Điện")]
    Dien,

    [Display(Name = "Hybrid")]
    Hybrid
}

// Vòng đời trạng thái: ChoDuyet -> (SanSang | TuChoi) -> DangThue -> SanSang -> BaoTri -> SanSang ...
// Admin là người duy nhất chuyển ChoDuyet -> SanSang/TuChoi.
// Owner chỉ được chuyển giữa SanSang <-> BaoTri (không tự duyệt xe của mình).
// DangThue do BookingService set khi có booking được xác nhận (sẽ làm ở Sprint 3).
public enum TrangThaiXe
{
    [Display(Name = "Chờ duyệt")]
    ChoDuyet,

    [Display(Name = "Sẵn sàng")]
    SanSang,

    [Display(Name = "Đang thuê")]
    DangThue,

    [Display(Name = "Bảo trì")]
    BaoTri,

    [Display(Name = "Từ chối")]
    TuChoi
}

public class Vehicle
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid OwnerId { get; set; }

    [ForeignKey(nameof(OwnerId))]
    public User Owner { get; set; } = null!;

    [Required, MaxLength(100)]
    [Display(Name = "Tên xe")]
    public string TenXe { get; set; } = string.Empty;

    [MaxLength(100)]
    [Display(Name = "Hãng xe")]
    public string? HangXe { get; set; }

    [Required, MaxLength(50)]
    [Display(Name = "Loại xe")]
    public string LoaiXe { get; set; } = string.Empty;

    [Required, MaxLength(15)]
    [Display(Name = "Biển số")]
    public string BienSo { get; set; } = string.Empty;

    [Range(1, 16)]
    [Display(Name = "Số chỗ ngồi")]
    public int SoChoNgoi { get; set; }

    [Required]
    [Display(Name = "Hộp số")]
    public HopSoXe HopSo { get; set; }

    [Required]
    [Display(Name = "Nhiên liệu")]
    public NhienLieuXe NhienLieu { get; set; }

    [Range(1990, 2100)]
    [Display(Name = "Năm sản xuất")]
    public int Nam { get; set; }

    [Required, MaxLength(100)]
    [Display(Name = "Khu vực")]
    public string KhuVuc { get; set; } = string.Empty;

    [MaxLength(250)]
    [Display(Name = "Địa chỉ cụ thể")]
    public string? DiaChiCuThe { get; set; }

    [Range(0, 100_000_000)]
    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Giá thuê / ngày (VNĐ)")]
    public decimal GiaThueMotNgay { get; set; }

    [MaxLength(2000)]
    [Display(Name = "Mô tả")]
    public string? MoTa { get; set; }

    [Required]
    public TrangThaiXe TrangThai { get; set; } = TrangThaiXe.ChoDuyet;

    [MaxLength(300)]
    public string? LyDoTuChoi { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ApprovedAt { get; set; }

    public ICollection<VehicleImage> HinhAnh { get; set; } = new List<VehicleImage>();

    public ICollection<Booking> DanhSachDatXe { get; set; } = new List<Booking>();

    public ICollection<Review> DanhGia { get; set; } = new List<Review>();
}
