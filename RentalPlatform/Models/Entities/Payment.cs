using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RentalPlatform.Models.Entities;

public enum LoaiThanhToan
{
    DatCoc,
    ThanhToanConLai
}

public enum PhuongThucThanhToan
{
    TienMat,
    ChuyenKhoanNganHang,
    ViDienTu
}

// Đây là mô phỏng thanh toán cho mục đích đồ án, KHÔNG tích hợp cổng thanh toán thật.
// Mọi Payment tạo ra đều ở trạng thái ThanhCong ngay lập tức (không có bước chờ xử lý thật).
public enum TrangThaiThanhToan
{
    ThanhCong,
    DaHoanTien
}

public class Payment
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid BookingId { get; set; }

    [ForeignKey(nameof(BookingId))]
    public Booking Booking { get; set; } = null!;

    [Required]
    public LoaiThanhToan LoaiThanhToan { get; set; }

    [Required]
    public PhuongThucThanhToan PhuongThuc { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal SoTien { get; set; }

    // Mã giao dịch mô phỏng, tự sinh, không phải mã thật từ cổng thanh toán
    [Required, MaxLength(50)]
    public string MaGiaoDich { get; set; } = string.Empty;

    [Required]
    public TrangThaiThanhToan TrangThai { get; set; } = TrangThaiThanhToan.ThanhCong;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
