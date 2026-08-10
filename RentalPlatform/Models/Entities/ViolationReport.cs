using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RentalPlatform.Models.Entities;

public enum TrangThaiBaoCaoViPham { ChoXuLy, DangXuLy, DaXuLy, TuChoi }
public enum HinhThucXuLyViPham { KhongXuLy, CanhCao, Khoa7Ngay, Khoa30Ngay, KhoaVoThoiHan }

public class ViolationReport
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ReporterId { get; set; }
    [ForeignKey(nameof(ReporterId))] public User Reporter { get; set; } = null!;
    public Guid ReportedUserId { get; set; }
    [ForeignKey(nameof(ReportedUserId))] public User ReportedUser { get; set; } = null!;
    public Guid? RentalOrderId { get; set; }
    [ForeignKey(nameof(RentalOrderId))] public RentalOrder? RentalOrder { get; set; }
    [Required, MaxLength(100)] public string LoaiViPham { get; set; } = string.Empty;
    [Required, MaxLength(2000)] public string MoTa { get; set; } = string.Empty;
    [MaxLength(500)] public string? BangChungUrl { get; set; }
    public TrangThaiBaoCaoViPham TrangThai { get; set; } = TrangThaiBaoCaoViPham.ChoXuLy;
    public HinhThucXuLyViPham HinhThucXuLy { get; set; }
    [MaxLength(2000)] public string? GhiChuAdmin { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }
}
