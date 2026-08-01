using Microsoft.EntityFrameworkCore;
using RentalPlatform.Models.Entities;

namespace RentalPlatform.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<VehicleImage> VehicleImages => Set<VehicleImage>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Contract> Contracts => Set<Contract>();
    public DbSet<Review> Reviews => Set<Review>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.Role).HasConversion<string>();
        });

        modelBuilder.Entity<Vehicle>(entity =>
        {
            entity.Property(v => v.HopSo).HasConversion<string>();
            entity.Property(v => v.NhienLieu).HasConversion<string>();
            entity.Property(v => v.TrangThai).HasConversion<string>();

            entity.HasIndex(v => v.BienSo).IsUnique();

            // Index phục vụ tìm kiếm & lọc theo khu vực / loại xe / giá (yêu cầu phi chức năng Sprint 2)
            entity.HasIndex(v => v.KhuVuc);
            entity.HasIndex(v => v.LoaiXe);
            entity.HasIndex(v => v.GiaThueMotNgay);
            entity.HasIndex(v => v.TrangThai);

            entity.HasOne(v => v.Owner)
                .WithMany(u => u.XeSoHuu)
                .HasForeignKey(v => v.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<VehicleImage>(entity =>
        {
            entity.HasOne(vi => vi.Vehicle)
                .WithMany(v => v.HinhAnh)
                .HasForeignKey(vi => vi.VehicleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Booking>(entity =>
        {
            entity.Property(b => b.TrangThai).HasConversion<string>();

            // Index phục vụ trực tiếp truy vấn KiemTraConTrongAsync (kiểm tra trùng lịch theo xe + khoảng ngày)
            entity.HasIndex(b => new { b.VehicleId, b.NgayBatDau, b.NgayKetThuc });
            entity.HasIndex(b => b.TrangThai);

            entity.HasOne(b => b.Vehicle)
                .WithMany(v => v.DanhSachDatXe)
                .HasForeignKey(b => b.VehicleId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(b => b.Customer)
                .WithMany(u => u.DonDaDat)
                .HasForeignKey(b => b.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.Property(p => p.LoaiThanhToan).HasConversion<string>();
            entity.Property(p => p.PhuongThuc).HasConversion<string>();
            entity.Property(p => p.TrangThai).HasConversion<string>();

            entity.HasIndex(p => p.MaGiaoDich).IsUnique();
            entity.HasIndex(p => p.BookingId);

            entity.HasOne(p => p.Booking)
                .WithMany(b => b.DanhSachThanhToan)
                .HasForeignKey(p => p.BookingId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Contract>(entity =>
        {
            entity.HasIndex(c => c.SoHopDong).IsUnique();

            // Quan hệ 1-1 thật sự: Contract dùng chính BookingId làm khoá chính (shared PK),
            // đảm bảo 1 Booking không thể có 2 Contract ở tầng database.
            entity.HasOne(c => c.Booking)
                .WithOne(b => b.Contract)
                .HasForeignKey<Contract>(c => c.BookingId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Review>(entity =>
        {
            // Quan hệ 1-0..1 với Booking: BookingId là khoá ngoại thường, nhưng unique index
            // đảm bảo mỗi Booking chỉ có tối đa 1 Review.
            entity.HasOne(r => r.Booking)
                .WithOne(b => b.Review)
                .HasForeignKey<Review>(r => r.BookingId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(r => r.Vehicle)
                .WithMany(v => v.DanhGia)
                .HasForeignKey(r => r.VehicleId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(r => r.Customer)
                .WithMany()
                .HasForeignKey(r => r.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}