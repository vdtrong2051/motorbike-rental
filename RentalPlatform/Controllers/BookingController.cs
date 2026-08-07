using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalPlatform.Models.ViewModels.Booking;
using RentalPlatform.Services;

namespace RentalPlatform.Controllers;

[Authorize]
public class BookingController : Controller
{
    private readonly IBookingService _bookingService;
    private readonly IVehicleService _vehicleService;
    private readonly IPaymentService _paymentService;

    public BookingController(IBookingService bookingService, IVehicleService vehicleService, IPaymentService paymentService)
    {
        _bookingService = bookingService;
        _vehicleService = vehicleService;
        _paymentService = paymentService;
    }

    // GET /Booking/Create?vehicleId={id} — form chọn ngày, chỉ dành cho Customer
    [Authorize(Roles = "Customer")]
    [HttpGet]
    public async Task<IActionResult> Create(Guid vehicleId)
    {
        var vehicle = await _vehicleService.GetDetailAsync(vehicleId);
        if (vehicle is null) return NotFound();
        if (vehicle.TrangThai != Models.Entities.TrangThaiXe.SanSang)
        {
            TempData["ErrorMessage"] = "Xe hiện không sẵn sàng cho thuê.";
            return RedirectToAction("Details", "Vehicle", new { id = vehicleId });
        }

        ViewBag.Vehicle = vehicle;
        return View(new BookingCreateViewModel { VehicleId = vehicleId });
    }

    [Authorize(Roles = "Customer")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BookingCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Vehicle = await _vehicleService.GetDetailAsync(model.VehicleId);
            return View(model);
        }

        var result = await _bookingService.CreateAsync(model, GetCurrentUserId());
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            ViewBag.Vehicle = await _vehicleService.GetDetailAsync(model.VehicleId);
            return View(model);
        }

        TempData["SuccessMessage"] = "Đặt xe thành công! Đơn của bạn đang chờ chủ xe xác nhận.";
        return RedirectToAction(nameof(MyBookings));
    }

    // GET /Booking/MyBookings — lịch sử đặt xe của Customer
    [HttpGet]
    public async Task<IActionResult> MyBookings()
    {
        var bookings = await _bookingService.GetMyBookingsAsync(GetCurrentUserId());
        return View(bookings);
    }

    // GET /Booking/Requests — đơn đặt xe của các xe thuộc Owner hiện tại
    [Authorize(Roles = "Owner")]
    [HttpGet]
    public async Task<IActionResult> Requests()
    {
        var bookings = await _bookingService.GetOwnerRequestsAsync(GetCurrentUserId());
        return View(bookings);
    }

    [Authorize(Roles = "Owner")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(Guid id)
    {
        var result = await _bookingService.XacNhanAsync(id, GetCurrentUserId());
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Success
            ? "Đã xác nhận đơn đặt xe. Hợp đồng điện tử đã được tự động sinh, mời hai bên vào ký."
            : result.Error;
        return RedirectToAction(nameof(Requests));
    }

    [Authorize(Roles = "Owner")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GiaoXe(Guid id)
    {
        var result = await _bookingService.XacNhanGiaoXeAsync(id, GetCurrentUserId());
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Success
            ? "Đã xác nhận giao xe cho khách."
            : result.Error;
        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize(Roles = "Owner")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> NhanXe(Guid id)
    {
        var result = await _bookingService.XacNhanTraXeAsync(id, GetCurrentUserId());
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Success
            ? "Đã xác nhận nhận lại xe. Đơn đã hoàn thành."
            : result.Error;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(Guid id, string? lyDo, string returnAction = nameof(MyBookings))
    {
        var result = await _bookingService.HuyAsync(id, GetCurrentUserId(), lyDo);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Success
            ? "Đã huỷ đơn đặt xe."
            : result.Error;

        return returnAction == nameof(Requests)
            ? RedirectToAction(nameof(Requests))
            : RedirectToAction(nameof(MyBookings));
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id)
    {
        var booking = await _bookingService.GetDetailAsync(id, GetCurrentUserId(), User.IsInRole("Admin"));
        if (booking is null) return NotFound();

        ViewBag.DanhSachThanhToan = await _paymentService.GetByBookingIdAsync(id);
        ViewBag.TongDaThanhToan = await _paymentService.GetTongDaThanhToanAsync(id);
        ViewBag.TyLeDatCoc = _paymentService.TyLeDatCoc;

        return View(booking);
    }

    private Guid GetCurrentUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}