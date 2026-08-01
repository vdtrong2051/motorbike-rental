using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalPlatform.Services;

namespace RentalPlatform.Controllers;

[Authorize]
public class ContractController : Controller
{
    private readonly IContractService _contractService;

    public ContractController(IContractService contractService)
    {
        _contractService = contractService;
    }

    // GET /Contract/Details/{bookingId} — xem nội dung hợp đồng + trạng thái ký 2 bên
    [HttpGet]
    public async Task<IActionResult> Details(Guid bookingId)
    {
        var contract = await _contractService.GetByBookingIdAsync(bookingId, GetCurrentUserId(), User.IsInRole("Admin"));
        if (contract is null) return NotFound();
        return View(contract);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Sign(Guid bookingId)
    {
        var result = await _contractService.KyHopDongAsync(bookingId, GetCurrentUserId());
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Success
            ? "Đã ký hợp đồng thành công."
            : result.Error;
        return RedirectToAction(nameof(Details), new { bookingId });
    }

    private Guid GetCurrentUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
