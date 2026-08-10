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

        var today = DateTime.UtcNow.Date;
        var month = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var year = new DateTime(DateTime.UtcNow.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var orderPayments = _db.RentalOrderPayments.AsNoTracking().Where(p => p.TrangThai == TrangThaiThanhToanDon.DaThanhToan);
        vm.TongDoanhThu = await orderPayments.SumAsync(p => (decimal?)p.SoTien) ?? 0;
        vm.DoanhThuHomNay = await orderPayments.Where(p => p.PaidAt >= today).SumAsync(p => (decimal?)p.SoTien) ?? 0;
        vm.DoanhThuThangNay = await orderPayments.Where(p => p.PaidAt >= month).SumAsync(p => (decimal?)p.SoTien) ?? 0;
        vm.DoanhThuNamNay = await orderPayments.Where(p => p.PaidAt >= year).SumAsync(p => (decimal?)p.SoTien) ?? 0;
        vm.DoanhThuPhiDichVu = await _db.RentalOrders.Where(o => o.TrangThaiThanhToan == TrangThaiThanhToanDon.DaThanhToan)
            .SumAsync(o => (decimal?)o.PhiDichVu) ?? 0;
        vm.BaoCaoChoXuLy = await _db.ViolationReports.CountAsync(r => r.TrangThai == TrangThaiBaoCaoViPham.ChoXuLy);
        vm.TongSoDon = await _db.RentalOrders.CountAsync();
        vm.SoDonChoXacNhan = await _db.RentalOrders.CountAsync(o => o.TrangThai == TrangThaiDonThue.ChoChuXeXacNhan);
        vm.SoDonDaXacNhan = await _db.RentalOrders.CountAsync(o => o.TrangThai == TrangThaiDonThue.DaXacNhan
            || o.TrangThai == TrangThaiDonThue.ChoThanhToan || o.TrangThai == TrangThaiDonThue.DaThanhToan);
        vm.SoDonDangThue = await _db.RentalOrders.CountAsync(o => o.TrangThai == TrangThaiDonThue.DangThue);
        vm.SoDonHoanThanh = await _db.RentalOrders.CountAsync(o => o.TrangThai == TrangThaiDonThue.HoanThanh);
        vm.SoDonDaHuy = await _db.RentalOrders.CountAsync(o => o.TrangThai == TrangThaiDonThue.DaHuy);

        vm.TyLeSuDungXe = vm.TongSoXe == 0 ? 0 : Math.Round((double)vm.SoXeDangThue / vm.TongSoXe * 100, 1);

        var diemDanhGia = await _db.Reviews.Select(r => r.SoSao).ToListAsync();
        vm.TongSoDanhGia = diemDanhGia.Count;
        vm.DiemDanhGiaTrungBinh = diemDanhGia.Count == 0 ? 0 : Math.Round(diemDanhGia.Average(), 2);

        // Doanh thu theo tháng — 6 tháng gần nhất, dựa trên các Payment thành công
        var moc6ThangTruoc = DateTime.UtcNow.AddMonths(-5);
        var moc6ThangTruocDauThang = new DateTime(moc6ThangTruoc.Year, moc6ThangTruoc.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var thanhToanGanDay = await _db.RentalOrderPayments
            .Where(p => p.TrangThai == TrangThaiThanhToanDon.DaThanhToan && p.PaidAt >= moc6ThangTruocDauThang)
            .Select(p => new { CreatedAt = p.PaidAt!.Value, p.SoTien })
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
        vm.TopXeChoThue = await _db.RentalOrderItems
            .Where(i => i.RentalOrder.TrangThai != TrangThaiDonThue.DaHuy)
            .GroupBy(i => new { i.VehicleId, i.TenXe })
            .Select(g => new TopVehiclePoint { TenXe = g.Key.TenXe, SoLanThue = g.Count(), DoanhThu = g.Sum(i => i.ThanhTien) })
            .OrderByDescending(x => x.SoLanThue).Take(5).ToListAsync();
        vm.XeItDuocThue = await _db.Vehicles.Select(v => new TopVehiclePoint
            { TenXe = v.TenXe, SoLanThue = _db.RentalOrderItems.Count(i => i.VehicleId == v.Id && i.RentalOrder.TrangThai != TrangThaiDonThue.DaHuy) })
            .OrderBy(x => x.SoLanThue).ThenBy(x => x.TenXe).Take(5).ToListAsync();

        return vm;
    }
}
