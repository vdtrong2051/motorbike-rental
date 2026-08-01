using Microsoft.EntityFrameworkCore;
using RentalPlatform.Data;
using RentalPlatform.Models.Entities;
using RentalPlatform.Models.ViewModels.Admin;

namespace RentalPlatform.Services;

public class DashboardService : IDashboardService
{
    private readonly AppDbContext _db;

    public DashboardService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<AdminDashboardViewModel> GetAdminDashboardAsync()
    {
        var vm = new AdminDashboardViewModel
        {
            TongNguoiDung = await _db.Users.CountAsync(),
            SoCustomer = await _db.Users.CountAsync(u => u.Role == UserRole.Customer),
            SoOwner = await _db.Users.CountAsync(u => u.Role == UserRole.Owner),

            TongSoXe = await _db.Vehicles.CountAsync(),
            SoXeSanSang = await _db.Vehicles.CountAsync(v => v.TrangThai == TrangThaiXe.SanSang),
            SoXeDangThue = await _db.Vehicles.CountAsync(v => v.TrangThai == TrangThaiXe.DangThue),
            SoXeBaoTri = await _db.Vehicles.CountAsync(v => v.TrangThai == TrangThaiXe.BaoTri),
            SoXeChoDuyet = await _db.Vehicles.CountAsync(v => v.TrangThai == TrangThaiXe.ChoDuyet),

            TongSoDon = await _db.Bookings.CountAsync(),
            SoDonChoXacNhan = await _db.Bookings.CountAsync(b => b.TrangThai == TrangThaiBooking.ChoXacNhan),
            SoDonDaXacNhan = await _db.Bookings.CountAsync(b => b.TrangThai == TrangThaiBooking.DaXacNhan),
            SoDonDangThue = await _db.Bookings.CountAsync(b => b.TrangThai == TrangThaiBooking.DangThue),
            SoDonHoanThanh = await _db.Bookings.CountAsync(b => b.TrangThai == TrangThaiBooking.HoanThanh),
            SoDonDaHuy = await _db.Bookings.CountAsync(b => b.TrangThai == TrangThaiBooking.DaHuy),
        };

        vm.TongDoanhThu = await _db.Payments
            .Where(p => p.TrangThai == TrangThaiThanhToan.ThanhCong)
            .SumAsync(p => (decimal?)p.SoTien) ?? 0;

        vm.TyLeSuDungXe = vm.TongSoXe == 0 ? 0 : Math.Round((double)vm.SoXeDangThue / vm.TongSoXe * 100, 1);

        var diemDanhGia = await _db.Reviews.Select(r => r.SoSao).ToListAsync();
        vm.TongSoDanhGia = diemDanhGia.Count;
        vm.DiemDanhGiaTrungBinh = diemDanhGia.Count == 0 ? 0 : Math.Round(diemDanhGia.Average(), 2);

        // Doanh thu theo tháng — 6 tháng gần nhất, dựa trên các Payment thành công
        var moc6ThangTruoc = DateTime.UtcNow.AddMonths(-5);
        var moc6ThangTruocDauThang = new DateTime(moc6ThangTruoc.Year, moc6ThangTruoc.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var thanhToanGanDay = await _db.Payments
            .Where(p => p.TrangThai == TrangThaiThanhToan.ThanhCong && p.CreatedAt >= moc6ThangTruocDauThang)
            .Select(p => new { p.CreatedAt, p.SoTien })
            .ToListAsync();

        vm.DoanhThuTheoThang = Enumerable.Range(0, 6)
            .Select(i => DateTime.UtcNow.AddMonths(-5 + i))
            .Select(thang => new RevenueByMonthPoint
            {
                Thang = thang.ToString("MM/yyyy"),
                DoanhThu = thanhToanGanDay
                    .Where(p => p.CreatedAt.Year == thang.Year && p.CreatedAt.Month == thang.Month)
                    .Sum(p => p.SoTien)
            })
            .ToList();

        // Top 5 xe theo doanh thu, tính trên các đơn đã DangThue hoặc HoanThanh (xe đã thực sự được giao)
        vm.TopXeChoThue = await _db.Bookings
            .Where(b => b.TrangThai == TrangThaiBooking.HoanThanh || b.TrangThai == TrangThaiBooking.DangThue)
            .GroupBy(b => new { b.VehicleId, b.Vehicle.TenXe })
            .Select(g => new TopVehiclePoint
            {
                TenXe = g.Key.TenXe,
                SoLanThue = g.Count(),
                DoanhThu = g.Sum(b => b.TongTien)
            })
            .OrderByDescending(x => x.DoanhThu)
            .Take(5)
            .ToListAsync();

        return vm;
    }
}
