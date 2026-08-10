using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalPlatform.Models.Entities;
using RentalPlatform.Models.ViewModels.RentalOrder;
using RentalPlatform.Services;

namespace RentalPlatform.Controllers;

[Authorize(Roles = "Customer")]
public class OrderPaymentController : Controller
{
    private readonly IRentalOrderService _orders;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;

    public OrderPaymentController(IRentalOrderService orders, IConfiguration configuration, IWebHostEnvironment environment)
    {
        _orders = orders;
        _configuration = configuration;
        _environment = environment;
    }

    [HttpGet]
    public async Task<IActionResult> Index(Guid orderId)
    {
        var order = await _orders.GetDetailAsync(orderId, CurrentUserId);
        if (order is null) return NotFound();
        return View(order);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Select(PaymentSelectionViewModel model)
    {
        var result = await _orders.SelectPaymentAsync(model.OrderId, model.PhuongThuc, CurrentUserId);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Success
            ? model.PhuongThuc switch
            {
                PhuongThucThanhToanDon.ChuyenKhoan => "Đã tạo mã QR chuyển khoản. Hệ thống đang chờ xác nhận từ cổng thanh toán.",
                PhuongThucThanhToanDon.ThanhToanKhiNhanXe => "Đã chọn thanh toán khi nhận xe.",
                PhuongThucThanhToanDon.ViDienTu => "Đã tạo giao dịch ví điện tử và đang chờ cổng thanh toán xác nhận.",
                _ => "Đã tạo giao dịch thẻ và đang chờ cổng thanh toán xác nhận."
            }
            : result.Error;
        return RedirectToAction(nameof(Index), new { orderId = model.OrderId });
    }

    [HttpGet]
    public async Task<IActionResult> Status(Guid orderId)
    {
        var order = await _orders.GetDetailAsync(orderId, CurrentUserId);
        if (order is null) return NotFound();
        return Json(new
        {
            paid = order.TrangThaiThanhToan == TrangThaiThanhToanDon.DaThanhToan,
            status = order.TrangThaiThanhToan.ToString(),
            paidAt = order.PaidAt?.ToLocalTime().ToString("dd/MM/yyyy HH:mm")
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SimulatePaid(Guid orderId)
    {
        if (!_environment.IsDevelopment()) return NotFound();
        var order = await _orders.GetDetailAsync(orderId, CurrentUserId);
        if (order is null || order.PhuongThucThanhToan != PhuongThucThanhToanDon.ChuyenKhoan)
            return NotFound();
        var result = await _orders.MarkPaidAsync(order.Id, order.MaThamChieuThanhToan!, order.TongTien, "Giao dịch mô phỏng trong môi trường Development.");
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Success ? "Mô phỏng webhook thành công: đơn đã thanh toán." : result.Error;
        return RedirectToAction(nameof(Index), new { orderId });
    }

    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    [HttpPost("/api/payments/webhook")]
    public async Task<IActionResult> Webhook([FromBody] PaymentWebhookRequest request)
    {
        var expectedSecret = _configuration["Payment:WebhookSecret"];
        var suppliedSecret = Request.Headers["X-Webhook-Secret"].ToString();
        if (string.IsNullOrWhiteSpace(expectedSecret) || !string.Equals(expectedSecret, suppliedSecret, StringComparison.Ordinal))
            return Unauthorized(new { message = "Invalid webhook secret." });
        if (!string.Equals(request.Status, "PAID", StringComparison.OrdinalIgnoreCase))
            return Ok(new { received = true, updated = false });

        var result = await _orders.MarkPaidAsync(request.OrderId, request.Reference, request.Amount, request.GatewayNote);
        return result.Success
            ? Ok(new { received = true, updated = true })
            : BadRequest(new { received = true, updated = false, error = result.Error });
    }

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}

public class PaymentWebhookRequest
{
    public Guid OrderId { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? GatewayNote { get; set; }
}
