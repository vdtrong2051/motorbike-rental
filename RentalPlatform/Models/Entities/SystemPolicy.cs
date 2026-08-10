using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RentalPlatform.Models.Entities;

public class SystemPolicy
{
    [Key] public int Id { get; set; } = 1;
    [Column(TypeName = "decimal(5,2)"), Range(0, 100)] public decimal TyLeDatCoc { get; set; } = 20;
    [Column(TypeName = "decimal(5,2)"), Range(0, 100)] public decimal TyLePhiDichVu { get; set; } = 5;
    [Column(TypeName = "decimal(5,2)"), Range(0, 100)] public decimal HoanTienTruoc72Gio { get; set; } = 100;
    [Column(TypeName = "decimal(5,2)"), Range(0, 100)] public decimal HoanTienTruoc24Gio { get; set; } = 70;
    [Column(TypeName = "decimal(5,2)"), Range(0, 100)] public decimal HoanTienTrong24Gio { get; set; } = 30;
    [Column(TypeName = "decimal(18,2)"), Range(0, 100_000_000)] public decimal PhatTraMuonMoiGio { get; set; } = 100_000;
    [MaxLength(4000)] public string? QuyDinhViPham { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
