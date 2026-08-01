using System.ComponentModel.DataAnnotations;

namespace RentalPlatform.Models.ViewModels.Booking;

public class BookingCreateViewModel
{
    [Required]
    public Guid VehicleId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn ngày nhận xe")]
    [DataType(DataType.Date)]
    [Display(Name = "Ngày nhận xe")]
    public DateOnly NgayBatDau { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(1));

    [Required(ErrorMessage = "Vui lòng chọn ngày trả xe")]
    [DataType(DataType.Date)]
    [Display(Name = "Ngày trả xe")]
    public DateOnly NgayKetThuc { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(2));

    [MaxLength(500)]
    [Display(Name = "Ghi chú cho chủ xe (tuỳ chọn)")]
    public string? GhiChu { get; set; }
}
