using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalPlatform.Data;
using RentalPlatform.Models.Entities;
using RentalPlatform.Services;
using RentalPlatform.Services.Payments;

namespace RentalPlatform.Controllers;

/// <summary>
/// GD1 JSON endpoints. Demo results are NEVER proof of an actual bank charge.
/// Existing MVC /OrderPayment/Select and /OrderPayment/Status remain supported.
/// </summary>
[ApiController]
[Route("api/payments")]
[Authorize(Roles = "Customer")]
public sealed class PaymentApiController : ControllerBase
{
    private readonly IRentalOrderService _orders;
    private readonly IPaymentCore _payments;
    private readonly IDemoPaymentTokenService _tokens;
    private readonly AppDbContext _db;

    public PaymentApiController(IRentalOrderService orders, IPaymentCore payments,
        IDemoPaymentTokenService tokens, AppDbContext db)
    {
        _orders = orders;
        _payments = payments;
        _tokens = tokens;
        _db = db;
    }

    [HttpPost("attempts")]
    [ValidateAntiForgeryToken] // Cookie auth: never allow CSRF to create/change an attempt.
    public async Task<IActionResult> Select([FromBody] PaymentAttemptRequest request)
    {
        var result = await _payments.SelectAsync(request.OrderId, request.Method, CurrentUserId);
        if (!result.Success) return Conflict(new { error = result.Error });
        return Ok(new
        {
            orderId = result.Order!.Id,
            reference = result.Order.MaThamChieuThanhToan,
            status = result.Order.TrangThaiThanhToan.ToString(),
            amount = result.Order.TongTien,
            simulated = false
        });
    }

    [HttpGet("orders/{orderId:guid}/status")]
    public async Task<IActionResult> Status(Guid orderId)
    {
        var order = await _db.RentalOrders.AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == orderId && o.CustomerId == CurrentUserId);
        if (order is null) return NotFound();
        Response.Headers.CacheControl = "no-store";
        return Ok(new
        {
            orderId = order.Id,
            status = order.TrangThaiThanhToan.ToString(),
            paid = order.TrangThaiThanhToan == TrangThaiThanhToanDon.DaThanhToan,
            method = order.PhuongThucThanhToan?.ToString(),
            reference = order.MaThamChieuThanhToan,
            paidAt = order.PaidAt
        });
    }

    [HttpGet("demo/session")]
    public async Task<IActionResult> Session([FromQuery] Guid orderId)
    {
        if (!_tokens.Enabled) return NotFound();
        var attempt = await _payments.GetDemoSessionAsync(orderId, CurrentUserId);
        if (attempt is null) return Conflict(new { error = "Không có giao dịch demo đang chờ và còn hiệu lực." });
        var token = _tokens.Issue(attempt.OrderId, attempt.Reference, attempt.ExpiresAtUtc);
        Response.Headers.CacheControl = "no-store";
        Response.Headers["Referrer-Policy"] = "no-referrer";
        return Ok(new
        {
            simulated = true,
            message = "DEMO ONLY - No real money is transferred",
            token,
            orderId = attempt.OrderId,
            reference = attempt.Reference,
            method = attempt.Method.ToString(),
            amount = attempt.Amount,
            expiresAtUtc = attempt.ExpiresAtUtc,
            submitUrl = "/api/payments/demo/complete"
        });
    }

    [AllowAnonymous]
    [IgnoreAntiforgeryToken] // Signed, expiring token is the bearer capability, not a browser cookie.
    [HttpPost("demo/complete")]
    public async Task<IActionResult> CompleteDemo([FromBody] DemoCompletionRequest request)
    {
        if (!_tokens.TryRead(request.Token ?? "", out var orderId, out var reference))
            return Unauthorized(new { error = "Demo token không hợp lệ hoặc đã hết hạn." });
        if (request.Outcome is not ("PAID" or "FAILED"))
            return BadRequest(new { error = "Outcome phải là PAID hoặc FAILED." });

        var result = request.Outcome == "PAID"
            ? await ConfirmDemoAsync(orderId, reference)
            : await _payments.FailAsync(orderId, reference, null, PaymentSignalSource.Demo, "Declined in demo flow");
        if (!result.Success) return Conflict(new { error = result.Error });
        Response.Headers.CacheControl = "no-store";
        return Ok(new
        {
            simulated = true,
            result = request.Outcome,
            orderId,
            status = result.Order!.TrangThaiThanhToan.ToString(),
            paid = result.Order.TrangThaiThanhToan == TrangThaiThanhToanDon.DaThanhToan
        });
    }

    private async Task<RentalOrderOperationResult> ConfirmDemoAsync(Guid orderId, string reference)
    {
        // Amount is sourced SERVER-SIDE, never trusted from a QR client.
        var session = await _db.RentalOrderPayments.AsNoTracking()
            .Where(p => p.RentalOrderId == orderId && p.MaGiaoDich == reference)
            .Select(p => new { p.SoTien })
            .FirstOrDefaultAsync();
        return session is null ? new(false, "Giao dịch không tồn tại.")
            : await _payments.ConfirmAsync(orderId, reference, session.SoTien, PaymentSignalSource.Demo,
                "Signed demo confirmation; NOT real funds");
    }

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}

public sealed class PaymentAttemptRequest
{
    public Guid OrderId { get; set; }
    public PhuongThucThanhToanDon Method { get; set; }
}

public sealed class DemoCompletionRequest
{
    public string? Token { get; set; }
    public string? Outcome { get; set; }
}
