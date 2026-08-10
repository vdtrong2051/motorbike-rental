using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using RentalPlatform.Models.Entities;

namespace RentalPlatform.Models.ViewModels.Account;

public class ProfileViewModel
{
    [Required, MaxLength(100), Display(Name = "Họ và tên")]
    public string HoTen { get; set; } = string.Empty;
    [Required, EmailAddress, Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;
    [Required, Phone, MaxLength(15), Display(Name = "Số điện thoại")]
    public string? SDT { get; set; }
    [Required, RegularExpression(@"^[0-9]{9,12}$", ErrorMessage = "CCCD/CMND phải gồm 9 đến 12 chữ số"), MaxLength(20), Display(Name = "Số CCCD/CMND")]
    public string? SoCCCD { get; set; }
    [Required, MaxLength(30), Display(Name = "Số giấy phép lái xe")]
    public string? SoGPLX { get; set; }
    [Required, MaxLength(300), Display(Name = "Địa chỉ thường trú")]
    public string? DiaChi { get; set; }
    [Required, DataType(DataType.Date), Display(Name = "Ngày sinh")]
    public DateTime? NgaySinh { get; set; }
    [Required, MaxLength(20), Display(Name = "Giới tính")]
    public string? GioiTinh { get; set; }
    public string Role { get; set; } = string.Empty;
    public TrangThaiXacThucNguoiDung TrangThaiXacThuc { get; set; }
    public string? GhiChuXacThuc { get; set; }
    public bool HoSoDayDu { get; set; }
    public bool DangBiTamKhoa { get; set; }
    [Display(Name = "Ảnh CCCD mặt trước")]
    public IFormFile? AnhCCCDMatTruocUpload { get; set; }
    [Display(Name = "Ảnh CCCD mặt sau")]
    public IFormFile? AnhCCCDMatSauUpload { get; set; }
    public bool CoAnhCCCDMatTruoc { get; set; }
    public bool CoAnhCCCDMatSau { get; set; }
}
