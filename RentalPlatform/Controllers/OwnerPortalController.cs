using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalPlatform.Services;

namespace RentalPlatform.Controllers;

[Authorize(Roles = "Owner")]
public class OwnerPortalController : Controller
{
    private readonly IRentalOrderService _orders;

    public OwnerPortalController(IRentalOrderService orders) => _orders = orders;

    [HttpGet]
    public async Task<IActionResult> Dashboard() =>
        View(await _orders.GetOwnerDashboardAsync(CurrentUserId));

    [HttpGet]
    public IActionResult Calendar() => View();

    [HttpGet]
    public async Task<IActionResult> CalendarEvents(DateTime? start, DateTime? end) =>
        Json(await _orders.GetCalendarAsync(CurrentUserId, start, end));

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
