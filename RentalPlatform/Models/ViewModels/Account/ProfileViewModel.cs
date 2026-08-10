using System.ComponentModel.DataAnnotations;

namespace RentalPlatform.Models.ViewModels.Account;

public class ProfileViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập họ tên")]
    [MaxLength(100)]
    [Display(Name = "Họ và tên")]
    public string HoTen { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
    [MaxLength(15)]
    [Display(Name = "Số điện thoại")]
    public string? SDT { get; set; }

    [RegularExpression(@"^[0-9]{9,12}$", ErrorMessage = "CCCD/CMND phải gồm 9 đến 12 chữ số")]
    [MaxLength(20)]
    [Display(Name = "Số CCCD/CMND")]
    public string? SoCCCD { get; set; }

    [MaxLength(30)]
    [Display(Name = "Số giấy phép lái xe")]
    public string? SoGPLX { get; set; }

    [MaxLength(300)]
    [Display(Name = "Địa chỉ thường trú")]
    public string? DiaChi { get; set; }

    public string Role { get; set; } = string.Empty;
}
