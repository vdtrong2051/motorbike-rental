using System.ComponentModel.DataAnnotations;

namespace RentalPlatform.Models.ViewModels.Review;

public class ReviewCreateViewModel
{
    [Required]
    public Guid BookingId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn số sao")]
    [Range(1, 5, ErrorMessage = "Số sao phải từ 1 đến 5")]
    [Display(Name = "Số sao")]
    public int SoSao { get; set; } = 5;

    [MaxLength(1000)]
    [Display(Name = "Nhận xét (tuỳ chọn)")]
    public string? NhanXet { get; set; }
}
