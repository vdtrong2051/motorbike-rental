using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalPlatform.Models.ViewModels.Payment;
using RentalPlatform.Services;

namespace RentalPlatform.Controllers;

[Authorize(Roles = "Customer")]
public class PaymentController : Controller
{
    private readonly IPaymentService _paymentService;

    public PaymentController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DatCoc(PaymentCreateViewModel model)
    {
        var result = await _paymentService.DatCocAsync(model, GetCurrentUserId());
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Success
            ? $"Đặt cọc thành công {result.Payment!.SoTien:N0} đ (mô phỏng). Mã giao dịch: {result.Payment.MaGiaoDich}"
            : result.Error;
        return RedirectToAction("Details", "Booking", new { id = model.BookingId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ThanhToanConLai(PaymentCreateViewModel model)
    {
        var result = await _paymentService.ThanhToanConLaiAsync(model, GetCurrentUserId());
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Success
            ? $"Thanh toán phần còn lại thành công {result.Payment!.SoTien:N0} đ (mô phỏng). Mã giao dịch: {result.Payment.MaGiaoDich}"
            : result.Error;
        return RedirectToAction("Details", "Booking", new { id = model.BookingId });
    }

    private Guid GetCurrentUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
