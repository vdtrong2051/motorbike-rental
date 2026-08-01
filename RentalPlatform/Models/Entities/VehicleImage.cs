using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RentalPlatform.Models.Entities;

public class VehicleImage
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid VehicleId { get; set; }

    [ForeignKey(nameof(VehicleId))]
    public Vehicle Vehicle { get; set; } = null!;

    // Link ảnh (URL tuyệt đối do người dùng nhập), ví dụ: https://cdn.example.com/abc123.jpg
    [Required, MaxLength(2000)]
    public string DuongDanAnh { get; set; } = string.Empty;

    public bool LaAnhDaiDien { get; set; } = false;

    public int ThuTu { get; set; } = 0;
}
