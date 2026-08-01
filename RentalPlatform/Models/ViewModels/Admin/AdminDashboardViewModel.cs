namespace RentalPlatform.Models.ViewModels.Admin;

public class RevenueByMonthPoint
{
    public string Thang { get; set; } = string.Empty; // "MM/yyyy"
    public decimal DoanhThu { get; set; }
}

public class TopVehiclePoint
{
    public string TenXe { get; set; } = string.Empty;
    public int SoLanThue { get; set; }
    public decimal DoanhThu { get; set; }
}

public class AdminDashboardViewModel
{
    // Người dùng
    public int TongNguoiDung { get; set; }
    public int SoCustomer { get; set; }
    public int SoOwner { get; set; }

    // Xe
    public int TongSoXe { get; set; }
    public int SoXeSanSang { get; set; }
    public int SoXeDangThue { get; set; }
    public int SoXeBaoTri { get; set; }
    public int SoXeChoDuyet { get; set; }

    // Đơn đặt xe
    public int TongSoDon { get; set; }
    public int SoDonChoXacNhan { get; set; }
    public int SoDonDaXacNhan { get; set; }
    public int SoDonDangThue { get; set; }
    public int SoDonHoanThanh { get; set; }
    public int SoDonDaHuy { get; set; }

    // Doanh thu & tỷ lệ sử dụng xe (2 chỉ số Admin cần theo dõi theo use case)
    public decimal TongDoanhThu { get; set; }
    public double TyLeSuDungXe { get; set; } // % xe đang thuê / tổng số xe

    // Đánh giá
    public double DiemDanhGiaTrungBinh { get; set; }
    public int TongSoDanhGia { get; set; }

    public List<RevenueByMonthPoint> DoanhThuTheoThang { get; set; } = new();
    public List<TopVehiclePoint> TopXeChoThue { get; set; } = new();
}
