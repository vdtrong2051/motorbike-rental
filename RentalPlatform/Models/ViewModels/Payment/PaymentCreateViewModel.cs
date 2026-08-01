using System.ComponentModel.DataAnnotations;
using RentalPlatform.Models.Entities;

namespace RentalPlatform.Models.ViewModels.Payment;

public class PaymentCreateViewModel
{
    [Required]
    public Guid BookingId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn phương thức thanh toán")]
    [Display(Name = "Phương thức thanh toán")]
    public PhuongThucThanhToan PhuongThuc { get; set; }
}
