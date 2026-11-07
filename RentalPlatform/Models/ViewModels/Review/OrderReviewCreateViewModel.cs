using System.ComponentModel.DataAnnotations;

namespace RentalPlatform.Models.ViewModels.Review;

public sealed class OrderReviewCreateViewModel
{
    [Required]
    public Guid RentalOrderItemId { get; set; }

    [Range(1, 5, ErrorMessage = "Số sao phải từ 1 đến 5.")]
    [Display(Name = "Số sao")]
    public int SoSao { get; set; } = 5;

    [MaxLength(1000, ErrorMessage = "Nhận xét tối đa 1.000 ký tự.")]
    [Display(Name = "Nhận xét (tùy chọn)")]
    public string? NhanXet { get; set; }
}
