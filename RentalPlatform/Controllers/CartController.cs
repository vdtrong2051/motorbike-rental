using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalPlatform.Services;

namespace RentalPlatform.Controllers;

[Authorize(Roles = "Customer")]
public class CartController : Controller
{
    private readonly ICartService _cart;

    public CartController(ICartService cart) => _cart = cart;

    [HttpGet]
    public async Task<IActionResult> Index() => View(await _cart.GetAsync());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(Guid vehicleId, string? returnUrl = null)
    {
        var result = await _cart.AddAsync(vehicleId);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Success
            ? "Đã thêm xe vào giỏ hàng."
            : result.Error;
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return LocalRedirect(returnUrl);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(Guid vehicleId)
    {
        await _cart.RemoveAsync(vehicleId);
        TempData["SuccessMessage"] = "Đã bỏ xe khỏi giỏ hàng.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Clear()
    {
        _cart.Clear();
        TempData["SuccessMessage"] = "Đã làm trống giỏ hàng.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult Count() => Json(new { count = _cart.Count() });
}
