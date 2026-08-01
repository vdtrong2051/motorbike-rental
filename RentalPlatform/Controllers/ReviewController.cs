using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalPlatform.Models.Entities;
using RentalPlatform.Models.ViewModels.Review;
using RentalPlatform.Services;

namespace RentalPlatform.Controllers;

[Authorize(Roles = "Customer")]
public class ReviewController : Controller
{
    private readonly IReviewService _reviewService;
    private readonly IBookingService _bookingService;

    public ReviewController(IReviewService reviewService, IBookingService bookingService)
    {
        _reviewService = reviewService;
        _bookingService = bookingService;
    }

    // GET /Review/Create?bookingId={id} — chỉ dành cho khách của đúng đơn đó, đơn phải HoanThanh
    // và chưa được đánh giá trước đó.
    [HttpGet]
    public async Task<IActionResult> Create(Guid bookingId)
    {
        var currentUserId = GetCurrentUserId();
        var booking = await _bookingService.GetDetailAsync(bookingId, currentUserId, isAdmin: false);
        if (booking is null) return NotFound();
        if (booking.CustomerId != currentUserId) return Forbid();

        if (booking.TrangThai != TrangThaiBooking.HoanThanh)
        {
            TempData["ErrorMessage"] = "Chỉ có thể đánh giá sau khi chuyến đi đã hoàn thành.";
            return RedirectToAction("Details", "Booking", new { id = bookingId });
        }

        if (booking.Review is not null)
        {
            TempData["ErrorMessage"] = "Bạn đã đánh giá đơn này rồi.";
            return RedirectToAction("Details", "Booking", new { id = bookingId });
        }

        ViewBag.Booking = booking;
        return View(new ReviewCreateViewModel { BookingId = bookingId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ReviewCreateViewModel model)
    {
        var currentUserId = GetCurrentUserId();

        if (!ModelState.IsValid)
        {
            ViewBag.Booking = await _bookingService.GetDetailAsync(model.BookingId, currentUserId, isAdmin: false);
            return View(model);
        }

        var result = await _reviewService.CreateAsync(model, currentUserId);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            ViewBag.Booking = await _bookingService.GetDetailAsync(model.BookingId, currentUserId, isAdmin: false);
            return View(model);
        }

        TempData["SuccessMessage"] = "Cảm ơn bạn đã đánh giá chuyến đi!";
        return RedirectToAction("Details", "Booking", new { id = model.BookingId });
    }

    private Guid GetCurrentUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
