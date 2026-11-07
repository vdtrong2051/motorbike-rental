using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalPlatform.Models.ViewModels.Review;
using RentalPlatform.Services;

namespace RentalPlatform.Controllers;

[Authorize]
public sealed class OrderReviewController : Controller
{
    private readonly IOrderReviewService _reviews;
    public OrderReviewController(IOrderReviewService reviews) => _reviews = reviews;

    [HttpGet]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> Create(Guid itemId)
    {
        var eligibility = await _reviews.CheckEligibilityAsync(itemId, CurrentUserId);
        if (eligibility.Item is null) return NotFound();
        if (!eligibility.Allowed)
        {
            TempData["ErrorMessage"] = eligibility.Error;
            return RedirectToAction("Details", "RentalOrder", new { id = eligibility.Item.RentalOrderId });
        }
        ViewBag.Item = eligibility.Item;
        return View(new OrderReviewCreateViewModel { RentalOrderItemId = itemId });
    }

    [HttpPost]
    [Authorize(Roles = "Customer")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(OrderReviewCreateViewModel model)
    {
        var eligibility = await _reviews.CheckEligibilityAsync(model.RentalOrderItemId, CurrentUserId);
        if (eligibility.Item is null) return NotFound();
        if (!eligibility.Allowed)
        {
            TempData["ErrorMessage"] = eligibility.Error;
            return RedirectToAction("Details", "RentalOrder", new { id = eligibility.Item.RentalOrderId });
        }
        if (!ModelState.IsValid)
        {
            ViewBag.Item = eligibility.Item;
            return View(model);
        }
        var result = await _reviews.CreateAsync(model, CurrentUserId);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Không thể gửi đánh giá.");
            ViewBag.Item = eligibility.Item;
            return View(model);
        }
        TempData["SuccessMessage"] = "Đã ghi nhận đánh giá của bạn.";
        return RedirectToAction("Details", "RentalOrder", new { id = eligibility.Item.RentalOrderId });
    }

    [HttpGet]
    [Authorize(Roles = "Owner")]
    public async Task<IActionResult> OwnerReviews()
        => View(await _reviews.GetOwnerReviewsAsync(CurrentUserId));

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AdminReviews(int? stars)
        => View(await _reviews.GetAdminReviewsAsync(stars));

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetVisibility(Guid reviewId, bool visible, int? stars)
    {
        var success = await _reviews.SetVisibilityAsync(reviewId, visible, CurrentUserId);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] = success
            ? (visible ? "Đã công khai đánh giá." : "Đã ẩn đánh giá khỏi trang xe.")
            : "Không tìm thấy đánh giá hoặc bạn không có quyền.";
        return RedirectToAction(nameof(AdminReviews), new { stars });
    }

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
