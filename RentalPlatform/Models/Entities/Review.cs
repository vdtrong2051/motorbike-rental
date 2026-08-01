using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RentalPlatform.Models.Entities;

// Quan hệ 1-1 với Booking (giống Contract): mỗi Booking chỉ được đánh giá tối đa 1 lần.
// Chỉ Customer của chính đơn đó mới được tạo Review, và chỉ khi Booking đã HoanThanh
// (ràng buộc nghiệp vụ được kiểm tra ở tầng Service, không phải ở đây).
// BookingId là khoá ngoại thường (không phải khoá chính) — ràng buộc "chỉ 1 Review/Booking"
// được đảm bảo bằng unique index mà Fluent API tự sinh cho quan hệ HasOne/WithOne
// trong AppDbContext (giống cách làm với Contract).
public class Review
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [ForeignKey(nameof(Booking))]
    public Guid BookingId { get; set; }

    public Booking Booking { get; set; } = null!;

    // Denormalize VehicleId/CustomerId để truy vấn thống kê rating theo xe (trang chi tiết xe,
    // dashboard) không cần join qua Booking.
    [Required]
    public Guid VehicleId { get; set; }

    [ForeignKey(nameof(VehicleId))]
    public Vehicle Vehicle { get; set; } = null!;

    [Required]
    public Guid CustomerId { get; set; }

    [ForeignKey(nameof(CustomerId))]
    public User Customer { get; set; } = null!;

    [Required]
    [Range(1, 5, ErrorMessage = "Số sao phải từ 1 đến 5")]
    [Display(Name = "Số sao")]
    public int SoSao { get; set; }

    [MaxLength(1000)]
    [Display(Name = "Nhận xét")]
    public string? NhanXet { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
