using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RentalPlatform.Models.Entities;

public class Contract
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    // Quan hệ 1-1 với Booking: mỗi Booking chỉ sinh tối đa 1 Contract, dùng chính
    // BookingId làm khoá chính kiêm khoá ngoại (xem cấu hình trong AppDbContext).
    [ForeignKey(nameof(Booking))]
    public Guid BookingId { get; set; }

    public Booking Booking { get; set; } = null!;

    [Required, MaxLength(30)]
    public string SoHopDong { get; set; } = string.Empty;

    // Nội dung hợp đồng được sinh tự động (snapshot) từ thông tin Booking/Vehicle/User
    // tại thời điểm Owner xác nhận đơn — không tự thay đổi nếu các thông tin gốc đổi sau này.
    [Required]
    public string NoiDungHopDong { get; set; } = string.Empty;

    public bool DaKyKhach { get; set; } = false;

    public DateTime? NgayKyKhach { get; set; }

    public bool DaKyChuXe { get; set; } = false;

    public DateTime? NgayKyChuXe { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}