using System.ComponentModel.DataAnnotations;
using RentalPlatform.Models.Entities;

namespace RentalPlatform.Models.ViewModels.RentalOrder;

public class CartItemViewModel
{
    public Guid VehicleId { get; set; }
    public string TenXe { get; set; } = string.Empty;
    public string BienSo { get; set; } = string.Empty;
    public string LoaiXe { get; set; } = string.Empty;
    public string KhuVuc { get; set; } = string.Empty;
    public string? AnhDaiDien { get; set; }
    public decimal GiaThueMotNgay { get; set; }
    public Guid OwnerId { get; set; }
}

public class CartViewModel
{
    public List<CartItemViewModel> Items { get; set; } = new();
    public Guid? OwnerId => Items.FirstOrDefault()?.OwnerId;
}

public class CheckoutItemInput
{
    [Required]
    public Guid VehicleId { get; set; }

    public string TenXe { get; set; } = string.Empty;
    public string BienSo { get; set; } = string.Empty;
    public string LoaiXe { get; set; } = string.Empty;
    public string TinhTrang { get; set; } = string.Empty;
    public decimal GiaThueMotNgay { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn thời gian nhận xe.")]
    [Display(Name = "Thời gian nhận xe")]
    public DateTime NgayNhanXe { get; set; } = DateTime.Today.AddDays(1).AddHours(8);

    [Required(ErrorMessage = "Vui lòng chọn thời gian trả xe.")]
    [Display(Name = "Thời gian trả xe")]
    public DateTime NgayTraXe { get; set; } = DateTime.Today.AddDays(2).AddHours(8);
}

public class CheckoutViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập họ tên."), MaxLength(100)]
    [Display(Name = "Họ và tên")]
    public string HoTen { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập email."), EmailAddress, MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại."), Phone, MaxLength(15)]
    [Display(Name = "Số điện thoại")]
    public string SDT { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập số CCCD."), RegularExpression(@"^\d{9,12}$", ErrorMessage = "CCCD phải gồm 9-12 chữ số.")]
    [Display(Name = "Số CCCD/CMND")]
    public string SoCCCD { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập số giấy phép lái xe."), MaxLength(30)]
    [Display(Name = "Số giấy phép lái xe")]
    public string SoGPLX { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập địa chỉ."), MaxLength(300)]
    [Display(Name = "Địa chỉ thường trú")]
    public string DiaChi { get; set; } = string.Empty;

    [MaxLength(500)]
    [Display(Name = "Ghi chú cho chủ xe")]
    public string? GhiChu { get; set; }

    [MinLength(1, ErrorMessage = "Giỏ hàng không có xe.")]
    public List<CheckoutItemInput> Items { get; set; } = new();
}

public class OwnerRevenuePoint
{
    public string Label { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
}

public class OwnerDashboardViewModel
{
    public decimal DoanhThuHomNay { get; set; }
    public decimal DoanhThuThangNay { get; set; }
    public decimal DoanhThuNamNay { get; set; }
    public int DonChoXacNhan { get; set; }
    public int XeDangThue { get; set; }
    public List<OwnerRevenuePoint> DoanhThu12Thang { get; set; } = new();
    public List<Models.Entities.RentalOrder> DonGanDay { get; set; } = new();
}

public class RentalCalendarEventViewModel
{
    public string Title { get; set; } = string.Empty;
    public string Start { get; set; } = string.Empty;
    public string End { get; set; } = string.Empty;
    public string Color { get; set; } = "#1F6F52";
    public string Url { get; set; } = string.Empty;
}

public class PaymentSelectionViewModel
{
    public Guid OrderId { get; set; }
    public PhuongThucThanhToanDon PhuongThuc { get; set; }
}
