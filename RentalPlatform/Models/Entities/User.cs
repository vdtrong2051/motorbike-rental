using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RentalPlatform.Models.Entities;

public enum UserRole { Customer, Owner, Admin }

public enum TrangThaiXacThucNguoiDung { ChuaGui, ChoDuyet, DaXacThuc, TuChoi }

public class User
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(100)] public string HoTen { get; set; } = string.Empty;
    [Required, MaxLength(150)] public string Email { get; set; } = string.Empty;
    [MaxLength(15)] public string? SDT { get; set; }
    [MaxLength(20)] public string? SoCCCD { get; set; }
    [MaxLength(260)] public string? AnhCCCDMatTruoc { get; set; }
    [MaxLength(260)] public string? AnhCCCDMatSau { get; set; }
    [MaxLength(30)] public string? SoGPLX { get; set; }
    [MaxLength(300)] public string? DiaChi { get; set; }
    [Column(TypeName = "date")] public DateTime? NgaySinh { get; set; }
    [MaxLength(20)] public string? GioiTinh { get; set; }
    [Required] public string PasswordHash { get; set; } = string.Empty;
    [Required] public UserRole Role { get; set; } = UserRole.Customer;
    public TrangThaiXacThucNguoiDung TrangThaiXacThuc { get; set; } = TrangThaiXacThucNguoiDung.ChuaGui;
    [MaxLength(500)] public string? GhiChuXacThuc { get; set; }
    public DateTime? GuiXacThucAt { get; set; }
    public DateTime? XacThucAt { get; set; }
    [MaxLength(30)] public string? OAuthProvider { get; set; }
    [MaxLength(200)] public string? OAuthSubject { get; set; }
    public bool BiTamKhoa { get; set; }
    public DateTime? TamKhoaDen { get; set; }
    public int SoLanViPham { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [NotMapped]
    public bool HoSoDayDu => !string.IsNullOrWhiteSpace(HoTen)
        && !string.IsNullOrWhiteSpace(Email) && !string.IsNullOrWhiteSpace(SDT)
        && !string.IsNullOrWhiteSpace(SoCCCD)
        && !string.IsNullOrWhiteSpace(AnhCCCDMatTruoc) && !string.IsNullOrWhiteSpace(AnhCCCDMatSau)
        && !string.IsNullOrWhiteSpace(SoGPLX)
        && !string.IsNullOrWhiteSpace(DiaChi) && NgaySinh.HasValue
        && !string.IsNullOrWhiteSpace(GioiTinh);

    [NotMapped]
    public bool DangBiTamKhoa => BiTamKhoa && (!TamKhoaDen.HasValue || TamKhoaDen > DateTime.UtcNow);

    [NotMapped]
    public bool DuDieuKienGiaoDich => HoSoDayDu
        && TrangThaiXacThuc == TrangThaiXacThucNguoiDung.DaXacThuc && !DangBiTamKhoa;

    public ICollection<Vehicle> XeSoHuu { get; set; } = new List<Vehicle>();
    public ICollection<Booking> DonDaDat { get; set; } = new List<Booking>();
}
