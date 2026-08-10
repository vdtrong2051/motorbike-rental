using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RentalPlatform.Models.Entities;

public enum TrangThaiDonThue
{
    ChoChuXeXacNhan,
    DaXacNhan,
    ChoThanhToan,
    DaThanhToan,
    DangThue,
    HoanThanh,
    DaHuy
}

public enum PhuongThucThanhToanDon
{
    ThanhToanKhiNhanXe,
    ChuyenKhoan,
    ViDienTu,
    TheThanhToan
}

public enum TrangThaiThanhToanDon
{
    ChuaChon,
    ChoThanhToan,
    ChoThuTienKhiNhanXe,
    DaThanhToan,
    ThatBai,
    ChoHoanTien,
    DaHoanTien
}

/// <summary>
/// Đơn thuê là đơn vị checkout/thanh toán. Một đơn có thể chứa nhiều xe,
/// nhưng các xe phải thuộc cùng một chủ để hợp đồng và đối soát rõ ràng.
/// </summary>
public class RentalOrder
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(30)]
    public string MaDon { get; set; } = string.Empty;

    [Required]
    public Guid CustomerId { get; set; }

    [ForeignKey(nameof(CustomerId))]
    public User Customer { get; set; } = null!;

    [Required]
    public Guid OwnerId { get; set; }

    [ForeignKey(nameof(OwnerId))]
    public User Owner { get; set; } = null!;

    [Required, MaxLength(100)]
    public string HoTenKhach { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string EmailKhach { get; set; } = string.Empty;

    [Required, MaxLength(15)]
    public string SDTKhach { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string SoCCCD { get; set; } = string.Empty;

    [Required, MaxLength(30)]
    public string SoGPLX { get; set; } = string.Empty;

    [Required, MaxLength(300)]
    public string DiaChiKhach { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? GhiChu { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TongTien { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TienThue { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TienDatCoc { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal PhiDichVu { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal SoTienHoanDuKien { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal TyLeHoanTien { get; set; }

    [MaxLength(500)]
    public string? LyDoHuy { get; set; }

    public DateTime? CancelledAt { get; set; }

    public TrangThaiDonThue TrangThai { get; set; } = TrangThaiDonThue.ChoChuXeXacNhan;

    public PhuongThucThanhToanDon? PhuongThucThanhToan { get; set; }

    public TrangThaiThanhToanDon TrangThaiThanhToan { get; set; } = TrangThaiThanhToanDon.ChuaChon;

    [MaxLength(100)]
    public string? MaThamChieuThanhToan { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ConfirmedAt { get; set; }

    public DateTime? PaidAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public ICollection<RentalOrderItem> Items { get; set; } = new List<RentalOrderItem>();

    public RentalOrderContract? Contract { get; set; }

    public ICollection<RentalOrderPayment> Payments { get; set; } = new List<RentalOrderPayment>();
}

public class RentalOrderItem
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid RentalOrderId { get; set; }

    [ForeignKey(nameof(RentalOrderId))]
    public RentalOrder RentalOrder { get; set; } = null!;

    [Required]
    public Guid VehicleId { get; set; }

    [ForeignKey(nameof(VehicleId))]
    public Vehicle Vehicle { get; set; } = null!;

    [Required, MaxLength(100)]
    public string TenXe { get; set; } = string.Empty;

    [Required, MaxLength(15)]
    public string BienSo { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string LoaiXe { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string TinhTrangLucDat { get; set; } = string.Empty;

    public DateTime NgayNhanXe { get; set; }

    public DateTime NgayTraXe { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal DonGiaNgay { get; set; }

    public int SoNgayThue { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal ThanhTien { get; set; }
}

public class RentalOrderContract
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid RentalOrderId { get; set; }

    [ForeignKey(nameof(RentalOrderId))]
    public RentalOrder RentalOrder { get; set; } = null!;

    [Required, MaxLength(30)]
    public string SoHopDong { get; set; } = string.Empty;

    [Required]
    public string NoiDungHopDong { get; set; } = string.Empty;

    // Snapshot bên cho thuê tại thời điểm lập hợp đồng. Các giá trị này không đổi
    // khi chủ xe cập nhật hồ sơ sau đó.
    [Required, MaxLength(100)]
    public string HoTenChuXe { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string EmailChuXe { get; set; } = string.Empty;

    [MaxLength(15)]
    public string? SDTChuXe { get; set; }

    [MaxLength(20)]
    public string? SoCCCDChuXe { get; set; }

    [MaxLength(300)]
    public string? DiaChiChuXe { get; set; }

    public bool DaKyKhach { get; set; }
    public DateTime? NgayKyKhach { get; set; }
    public bool DaKyChuXe { get; set; }
    public DateTime? NgayKyChuXe { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class RentalOrderPayment
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid RentalOrderId { get; set; }

    [ForeignKey(nameof(RentalOrderId))]
    public RentalOrder RentalOrder { get; set; } = null!;

    [Required, MaxLength(60)]
    public string MaGiaoDich { get; set; } = string.Empty;

    public PhuongThucThanhToanDon PhuongThuc { get; set; }

    public TrangThaiThanhToanDon TrangThai { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal SoTien { get; set; }

    [MaxLength(200)]
    public string? GhiChuCongThanhToan { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PaidAt { get; set; }

    public DateTime? ReconciledAt { get; set; }

    [MaxLength(500)]
    public string? GhiChuDoiSoat { get; set; }
}
