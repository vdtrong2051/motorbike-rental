using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using RentalPlatform.Data;
using RentalPlatform.Models.Entities;
using RentalPlatform.Models.ViewModels.PaymentDemo;
using RentalPlatform.Services.Payments;

namespace RentalPlatform.Controllers;

/// <summary>
/// GD2 only renders payment experiences; PaymentCore remains the sole writer of payment state.
/// Demo links are signed bearer capabilities, not evidence of any real bank transfer.
/// </summary>
public sealed class PaymentDemoController : Controller
{
    private readonly IPaymentCore _payments;
    private readonly IDemoPaymentTokenService _tokens;
    private readonly AppDbContext _db;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;

    public PaymentDemoController(IPaymentCore payments, IDemoPaymentTokenService tokens,
        AppDbContext db, IConfiguration configuration, IWebHostEnvironment environment)
    {
        _payments = payments;
        _tokens = tokens;
        _db = db;
        _configuration = configuration;
        _environment = environment;
    }

    [HttpGet]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> Qr(Guid orderId)
    {
        SetPrivateHeaders();
        var session = _tokens.Enabled
            ? await _payments.GetDemoSessionAsync(orderId, CurrentUserId)
            : null;
        if (session is null) return NotFound();

        var baseUrl = ResolvePublicBaseUrl();
        if (baseUrl is null)
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                "Cấu hình Payment:PublicBaseUrl hợp lệ để tạo QR có thể quét trên điện thoại.");

        var token = _tokens.Issue(session.OrderId, session.Reference, session.ExpiresAtUtc);
        var relative = Url.Action(nameof(Checkout), "PaymentDemo", new { token });
        if (string.IsNullOrWhiteSpace(relative)) return StatusCode(500);
        var destination = baseUrl + relative;

        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(destination, QRCodeGenerator.ECCLevel.M);
        using var qr = new PngByteQRCode(data);
        var png = qr.GetGraphic(7, drawQuietZones: true);
        return File(png, "image/png");
    }

    [HttpGet]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> Start(Guid orderId)
    {
        SetPrivateHeaders();
        var session = _tokens.Enabled
            ? await _payments.GetDemoSessionAsync(orderId, CurrentUserId)
            : null;
        if (session is null) return NotFound();
        var token = _tokens.Issue(session.OrderId, session.Reference, session.ExpiresAtUtc);
        return RedirectToAction(nameof(Checkout), new { token });
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Checkout([FromQuery] string? token)
    {
        SetPrivateHeaders();
        // No CDN, remote images or third-party analytics may see a token from this page.
        Response.Headers["Content-Security-Policy"] =
            "default-src 'none'; style-src 'self'; script-src 'self'; connect-src 'self'; img-src 'self'; " +
            "base-uri 'none'; form-action 'none'; frame-ancestors 'none'";

        var invalid = new PaymentDemoCheckoutViewModel
        {
            IsValid = false,
            Message = "Liên kết thanh toán mô phỏng không hợp lệ, đã hết hạn hoặc giao dịch không còn hiệu lực."
        };
        if (string.IsNullOrEmpty(token) || !_tokens.TryRead(token, out var orderId, out var reference))
        {
            Response.StatusCode = StatusCodes.Status410Gone;
            return View(invalid);
        }

        // Read-only view; POST /api/payments/demo/complete alone changes payment state.
        var payment = await _db.RentalOrderPayments.AsNoTracking()
            .Include(p => p.RentalOrder)
            .FirstOrDefaultAsync(p => p.RentalOrderId == orderId && p.MaGiaoDich == reference);
        var pendingMinutes = Math.Clamp(_configuration.GetValue("Payment:PendingMinutes", 30), 1, 1440);
        if (payment is null || !string.Equals(payment.RentalOrder.MaThamChieuThanhToan,
                reference, StringComparison.Ordinal)
            || payment.RentalOrder.TrangThai != TrangThaiDonThue.ChoThanhToan
            || payment.RentalOrder.TrangThaiThanhToan != TrangThaiThanhToanDon.ChoThanhToan
            || payment.TrangThai != TrangThaiThanhToanDon.ChoThanhToan
            || !PaymentCore.IsDemoMethod(payment.PhuongThuc)
            || DateTime.UtcNow >= DateTime.SpecifyKind(payment.CreatedAt, DateTimeKind.Utc).AddMinutes(pendingMinutes))
        {
            Response.StatusCode = StatusCodes.Status410Gone;
            return View(invalid);
        }

        var model = new PaymentDemoCheckoutViewModel
        {
            IsValid = true,
            Token = token,
            OrderCode = payment.RentalOrder.MaDon,
            Reference = payment.MaGiaoDich,
            Amount = payment.SoTien,
            Method = payment.PhuongThuc switch
            {
                PhuongThucThanhToanDon.ChuyenKhoan => "QR chuyển khoản mô phỏng",
                PhuongThucThanhToanDon.ViDienTu => "Ví điện tử mô phỏng",
                PhuongThucThanhToanDon.TheThanhToan => "Thẻ thanh toán mô phỏng",
                _ => "Thanh toán mô phỏng"
            },
            ExpiresAtUtc = DateTime.SpecifyKind(payment.CreatedAt, DateTimeKind.Utc).AddMinutes(pendingMinutes)
        };
        return View(model);
    }

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private void SetPrivateHeaders()
    {
        Response.Headers.CacheControl = "no-store, private, max-age=0";
        Response.Headers.Pragma = "no-cache";
        Response.Headers["Referrer-Policy"] = "no-referrer";
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        Response.Headers["X-Robots-Tag"] = "noindex, nofollow";
    }

    private string? ResolvePublicBaseUrl()
    {
        var configured = _configuration["Payment:PublicBaseUrl"]?.Trim().TrimEnd('/');
        if (!string.IsNullOrEmpty(configured))
        {
            if (!Uri.TryCreate(configured, UriKind.Absolute, out var parsed)
                || !string.IsNullOrEmpty(parsed.UserInfo) || !string.IsNullOrEmpty(parsed.Query)
                || !string.IsNullOrEmpty(parsed.Fragment) || parsed.AbsolutePath != "/"
                || (parsed.Scheme != Uri.UriSchemeHttps && !(_environment.IsDevelopment() && parsed.Scheme == Uri.UriSchemeHttp)))
                return null;
            return parsed.GetLeftPart(UriPartial.Authority);
        }
        // Dev convenience only; use Payment:PublicBaseUrl for a real phone-accessible URL.
        return _environment.IsDevelopment() ? $"{Request.Scheme}://{Request.Host}" : null;
    }
}
