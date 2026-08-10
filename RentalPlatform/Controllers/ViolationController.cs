using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalPlatform.Data;
using RentalPlatform.Models.Entities;

namespace RentalPlatform.Controllers;

[Authorize]
public class ViolationController : Controller
{
    private readonly AppDbContext _db;
    public ViolationController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Report(Guid orderId)
    {
        var userId = CurrentUserId;
        var order = await _db.RentalOrders.AsNoTracking().Include(o => o.Customer).Include(o => o.Owner)
            .FirstOrDefaultAsync(o => o.Id == orderId && (o.CustomerId == userId || o.OwnerId == userId));
        if (order is null) return NotFound();
        ViewBag.Order = order;
        ViewBag.ReportedUser = order.CustomerId == userId ? order.Owner : order.Customer;
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Report(Guid orderId, string loaiViPham, string moTa, string? bangChungUrl)
    {
        var userId = CurrentUserId;
        var order = await _db.RentalOrders.FirstOrDefaultAsync(o => o.Id == orderId && (o.CustomerId == userId || o.OwnerId == userId));
        if (order is null) return NotFound();
        if (string.IsNullOrWhiteSpace(loaiViPham) || string.IsNullOrWhiteSpace(moTa))
        { TempData["ErrorMessage"] = "Vui lòng nhập loại vi phạm và mô tả."; return RedirectToAction(nameof(Report), new { orderId }); }
        if (!string.IsNullOrWhiteSpace(bangChungUrl) && (!Uri.TryCreate(bangChungUrl, UriKind.Absolute, out var evidenceUri)
            || (evidenceUri.Scheme != Uri.UriSchemeHttp && evidenceUri.Scheme != Uri.UriSchemeHttps)))
        { TempData["ErrorMessage"] = "Liên kết bằng chứng phải bắt đầu bằng http:// hoặc https://."; return RedirectToAction(nameof(Report), new { orderId }); }
        _db.ViolationReports.Add(new ViolationReport
        {
            ReporterId = userId, ReportedUserId = order.CustomerId == userId ? order.OwnerId : order.CustomerId,
            RentalOrderId = order.Id, LoaiViPham = loaiViPham.Trim(), MoTa = moTa.Trim(),
            BangChungUrl = string.IsNullOrWhiteSpace(bangChungUrl) ? null : bangChungUrl.Trim()
        });
        await _db.SaveChangesAsync(); TempData["SuccessMessage"] = "Báo cáo đã được gửi tới Admin.";
        return RedirectToAction(nameof(MyReports));
    }

    [HttpGet]
    public async Task<IActionResult> MyReports() => View(await _db.ViolationReports.AsNoTracking()
        .Include(r => r.ReportedUser).Include(r => r.RentalOrder)
        .Where(r => r.ReporterId == CurrentUserId).OrderByDescending(r => r.CreatedAt).ToListAsync());

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
