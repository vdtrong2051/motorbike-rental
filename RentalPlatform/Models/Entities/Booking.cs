using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RentalPlatform.Models.Entities;

// Vòng đời: ChoXacNhan -> DaXacNhan -> DangThue -> HoanThanh
//                      -> DaHuy (huỷ/từ chối, có thể xảy ra từ ChoXacNhan hoặc DaXacNhan)
// Ràng buộc nghiệp vụ cốt lõi: một xe không được có 2 booking trùng khoảng ngày
// ở trạng thái DaXacNhan hoặc DangThue — đây là 2 trạng thái duy nhất được tính
// khi kiểm tra lịch trùng lặp (BookingService.KiemTraConTrongAsync).
// Sprint 4 đã bổ sung transition DaXacNhan -> DangThue (xác nhận giao xe, yêu cầu hợp đồng
// đã ký đủ 2 bên + đã thanh toán đủ tổng tiền) và DangThue -> HoanThanh (xác nhận nhận lại xe).
public enum TrangThaiBooking
{
    ChoXacNhan,
    DaXacNhan,
    DangThue,
    HoanThanh,
    DaHuy
}

public class Booking
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid VehicleId { get; set; }

    [ForeignKey(nameof(VehicleId))]
    public Vehicle Vehicle { get; set; } = null!;

    [Required]
    public Guid CustomerId { get; set; }

    [ForeignKey(nameof(CustomerId))]
    public User Customer { get; set; } = null!;

    [Required]
    [Display(Name = "Ngày nhận xe")]
    public DateOnly NgayBatDau { get; set; }

    [Required]
    [Display(Name = "Ngày trả xe")]
    public DateOnly NgayKetThuc { get; set; }

    // Snapshot giá thuê/ngày tại thời điểm đặt, để giá xe đổi sau này không ảnh hưởng
    // tới tổng tiền của các booking đã tạo trước đó.
    [Column(TypeName = "decimal(18,2)")]
    public decimal GiaThueMotNgayLucDat { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TongTien { get; set; }

    [Required]
    public TrangThaiBooking TrangThai { get; set; } = TrangThaiBooking.ChoXacNhan;

    [MaxLength(500)]
    [Display(Name = "Ghi chú")]
    public string? GhiChu { get; set; }

    [MaxLength(300)]
    public string? LyDoHuy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // Quan hệ 1-0..1: mỗi Booking chỉ sinh tối đa 1 Contract, được tự động tạo khi
    // Owner xác nhận đơn (BookingService.XacNhanAsync).
    public Contract? Contract { get; set; }

    // Quan hệ 1-n: 1 Booking có thể có nhiều Payment (đặt cọc + thanh toán phần còn lại)
    public ICollection<Payment> DanhSachThanhToan { get; set; } = new List<Payment>();

    // Quan hệ 1-0..1: mỗi Booking chỉ sinh tối đa 1 Review, chỉ tạo được sau khi
    // Booking đã HoanThanh (Sprint 5 — ReviewService.CreateAsync).
    public Review? Review { get; set; }
}