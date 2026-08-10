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
    public DbSet<RentalOrder> RentalOrders => Set<RentalOrder>();
    public DbSet<RentalOrderItem> RentalOrderItems => Set<RentalOrderItem>();
    public DbSet<RentalOrderContract> RentalOrderContracts => Set<RentalOrderContract>();
    public DbSet<RentalOrderPayment> RentalOrderPayments => Set<RentalOrderPayment>();
    public DbSet<SystemPolicy> SystemPolicies => Set<SystemPolicy>();
    public DbSet<ViolationReport> ViolationReports => Set<ViolationReport>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.Role).HasConversion<string>();
            entity.Property(u => u.TrangThaiXacThuc).HasConversion<string>();
            entity.HasIndex(u => new { u.OAuthProvider, u.OAuthSubject }).IsUnique();
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

        modelBuilder.Entity<RentalOrder>(entity =>
        {
            entity.Property(o => o.TrangThai).HasConversion<string>();
            entity.Property(o => o.PhuongThucThanhToan).HasConversion<string>();
            entity.Property(o => o.TrangThaiThanhToan).HasConversion<string>();
            entity.HasIndex(o => o.MaDon).IsUnique();
            entity.HasIndex(o => new { o.CustomerId, o.CreatedAt });
            entity.HasIndex(o => new { o.OwnerId, o.CreatedAt });

            entity.HasOne(o => o.Customer)
                .WithMany()
                .HasForeignKey(o => o.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(o => o.Owner)
                .WithMany()
                .HasForeignKey(o => o.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RentalOrderItem>(entity =>
        {
            entity.HasIndex(i => new { i.VehicleId, i.NgayNhanXe, i.NgayTraXe });

            entity.HasOne(i => i.RentalOrder)
                .WithMany(o => o.Items)
                .HasForeignKey(i => i.RentalOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(i => i.Vehicle)
                .WithMany()
                .HasForeignKey(i => i.VehicleId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RentalOrderContract>(entity =>
        {
            entity.HasIndex(c => c.SoHopDong).IsUnique();
            entity.HasIndex(c => c.RentalOrderId).IsUnique();
            entity.HasOne(c => c.RentalOrder)
                .WithOne(o => o.Contract)
                .HasForeignKey<RentalOrderContract>(c => c.RentalOrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RentalOrderPayment>(entity =>
        {
            entity.Property(p => p.PhuongThuc).HasConversion<string>();
            entity.Property(p => p.TrangThai).HasConversion<string>();
            entity.HasIndex(p => p.MaGiaoDich).IsUnique();
            entity.HasIndex(p => p.RentalOrderId);
            entity.HasOne(p => p.RentalOrder)
                .WithMany(o => o.Payments)
                .HasForeignKey(p => p.RentalOrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ViolationReport>(entity =>
        {
            entity.Property(r => r.TrangThai).HasConversion<string>();
            entity.Property(r => r.HinhThucXuLy).HasConversion<string>();
            entity.HasIndex(r => new { r.TrangThai, r.CreatedAt });
            entity.HasOne(r => r.Reporter).WithMany().HasForeignKey(r => r.ReporterId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(r => r.ReportedUser).WithMany().HasForeignKey(r => r.ReportedUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(r => r.RentalOrder).WithMany().HasForeignKey(r => r.RentalOrderId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
