using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalPlatform.Models.Entities;
using RentalPlatform.Models.ViewModels.RentalOrder;
using RentalPlatform.Services;
using RentalPlatform.Services.Payments;
using System.Security.Cryptography;
using System.Text;

namespace RentalPlatform.Controllers;

[Authorize(Roles = "Customer")]
public class OrderPaymentController : Controller
{
    private readonly IRentalOrderService _orders;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;
    private readonly IPaymentCore _paymentCore;

    public OrderPaymentController(IRentalOrderService orders, IConfiguration configuration,
        IWebHostEnvironment environment, IPaymentCore paymentCore)
    {
        _orders = orders;
        _configuration = configuration;
        _environment = environment;
        _paymentCore = paymentCore;
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
        Response.Headers.CacheControl = "no-store";
        return Json(new
        {
            paid = order.TrangThaiThanhToan == TrangThaiThanhToanDon.DaThanhToan,
            status = order.TrangThaiThanhToan.ToString(),
            orderStatus = order.TrangThai.ToString(),
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
        // A placeholder secret must never become an enabled production webhook.
        if (string.IsNullOrWhiteSpace(expectedSecret) || string.IsNullOrWhiteSpace(suppliedSecret)
            || expectedSecret.Length < 32
            || expectedSecret == "change-this-secret-before-production"
            || !CryptographicOperations.FixedTimeEquals(
                SHA256.HashData(Encoding.UTF8.GetBytes(expectedSecret)),
                SHA256.HashData(Encoding.UTF8.GetBytes(suppliedSecret))))
            return Unauthorized(new { message = "Invalid webhook credentials." });

        RentalOrderOperationResult result;
        if (string.Equals(request.Status, "PAID", StringComparison.OrdinalIgnoreCase))
            result = await _orders.MarkPaidAsync(request.OrderId, request.Reference, request.Amount, request.GatewayNote);
        else if (string.Equals(request.Status, "FAILED", StringComparison.OrdinalIgnoreCase))
            result = await _paymentCore.FailAsync(request.OrderId, request.Reference, request.Amount,
                PaymentSignalSource.Webhook, request.GatewayNote);
        else
            return Ok(new { received = true, updated = false });
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
