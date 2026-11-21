using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalPlatform.Models.Entities;
using RentalPlatform.Models.ViewModels.Vehicle;
using RentalPlatform.Services;
using RentalPlatform.Services.Availability;

namespace RentalPlatform.Controllers;

public class VehicleController : Controller
{
    private readonly IVehicleService _vehicleService;
    private readonly IOrderReviewService _orderReviews;
    private readonly IVehicleAvailabilityService _availability;

    public VehicleController(IVehicleService vehicleService, IOrderReviewService orderReviews,
        IVehicleAvailabilityService availability)
    {
        _vehicleService = vehicleService;
        _orderReviews = orderReviews;
        _availability = availability;
    }

    // GET /Vehicle/Index — tìm kiếm & lọc xe công khai (khu vực / loại xe / khoảng giá), có phân trang
    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] VehicleSearchViewModel filter)
    {
        filter.ChiXeSanSang = true;
        var result = await _vehicleService.SearchAsync(filter);
        return View(result);
    }

    // GET /Vehicle/Details/{id} — xem chi tiết xe, công khai
    [HttpGet]
    public async Task<IActionResult> Details(Guid id)
    {
        var vehicle = await _vehicleService.GetDetailAsync(id);
        if (vehicle is null) return NotFound();
        ViewBag.NewReviews = await _orderReviews.GetPublicByVehicleAsync(id);
        return View(vehicle);
    }

    // Public availability only. Never return customer identities or contract details.
    [HttpGet]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Availability(Guid vehicleId, DateTime? start, DateTime? end)
    {
        var from = start ?? DateTime.Today;
        var to = end ?? from.AddDays(42);
        if (vehicleId == Guid.Empty || to <= from || (to - from).TotalDays > 370)
            return BadRequest(new { error = "Khoảng xem lịch không hợp lệ." });
        var vehicle = await _vehicleService.GetDetailAsync(vehicleId);
        if (vehicle is null) return NotFound();
        var windows = await _availability.GetBusyWindowsAsync(vehicleId, from, to);
        return Json(windows.Select(w => new
        {
            title = "Đã có lịch thuê",
            start = w.Start.ToString("yyyy-MM-ddTHH:mm:ss"),
            end = w.End.ToString("yyyy-MM-ddTHH:mm:ss"),
            color = "#b54343"
        }));
    }

    [HttpGet]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> CheckAvailability(Guid vehicleId, DateTime start, DateTime end)
    {
        if (vehicleId == Guid.Empty || start < DateTime.Now.AddMinutes(-5)
            || end <= start || (end - start).TotalDays > 365)
            return BadRequest(new { available = false, message = "Khoảng thời gian nhận/trả không hợp lệ." });
        var available = await _availability.IsBookableAsync(vehicleId, start, end);
        return Json(new
        {
            available,
            message = available ? "Xe còn trống trong khoảng thời gian đã chọn."
                : "Xe không khả dụng hoặc đã có lịch thuê trùng khoảng này."
        });
    }

    [Authorize(Roles = "Owner")]
    [HttpGet]
    public IActionResult Create() => View(new VehicleCreateViewModel());

    [Authorize(Roles = "Owner")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(VehicleCreateViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await _vehicleService.CreateAsync(model, GetCurrentUserId());
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View(model);
        }

        TempData["SuccessMessage"] = "Đăng xe thành công! Xe của bạn đang chờ Admin duyệt.";
        return RedirectToAction(nameof(MyVehicles));
    }

    [Authorize(Roles = "Owner")]
    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var vehicle = await _vehicleService.GetDetailAsync(id);
        if (vehicle is null) return NotFound();
        if (vehicle.OwnerId != GetCurrentUserId()) return Forbid();

        var model = new VehicleEditViewModel
        {
            Id = vehicle.Id,
            TenXe = vehicle.TenXe,
            HangXe = vehicle.HangXe ?? string.Empty,
            LoaiXe = vehicle.LoaiXe,
            BienSo = vehicle.BienSo,
            SoChoNgoi = vehicle.SoChoNgoi,
            HopSo = vehicle.HopSo,
            NhienLieu = vehicle.NhienLieu,
            Nam = vehicle.Nam,
            KhuVuc = vehicle.KhuVuc,
            DiaChiCuThe = vehicle.DiaChiCuThe,
            GiaThueMotNgay = vehicle.GiaThueMotNgay,
            MoTa = vehicle.MoTa
        };

        ViewBag.Vehicle = vehicle;
        return View(model);
    }

    [Authorize(Roles = "Owner")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(VehicleEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var vehicle = await _vehicleService.GetDetailAsync(model.Id);
            ViewBag.Vehicle = vehicle;
            return View(model);
        }

        var result = await _vehicleService.UpdateAsync(model, GetCurrentUserId());
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            var vehicle = await _vehicleService.GetDetailAsync(model.Id);
            ViewBag.Vehicle = vehicle;
            return View(model);
        }

        TempData["SuccessMessage"] = "Cập nhật xe thành công! Nếu xe đã được duyệt trước đó, xe sẽ chờ Admin duyệt lại.";
        return RedirectToAction(nameof(MyVehicles));
    }

    [Authorize(Roles = "Owner")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _vehicleService.DeleteAsync(id, GetCurrentUserId());
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Success
            ? "Đã xoá xe."
            : result.Error;
        return RedirectToAction(nameof(MyVehicles));
    }

    [Authorize(Roles = "Owner")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteImage(Guid vehicleId, Guid imageId)
    {
        var result = await _vehicleService.DeleteImageAsync(vehicleId, imageId, GetCurrentUserId());
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Success
            ? "Đã xoá ảnh."
            : result.Error;
        return RedirectToAction(nameof(Edit), new { id = vehicleId });
    }

    [Authorize(Roles = "Owner")]
    [HttpGet]
    public async Task<IActionResult> MyVehicles()
    {
        var vehicles = await _vehicleService.GetMyVehiclesAsync(GetCurrentUserId());
        return View(vehicles);
    }

    [Authorize(Roles = "Owner")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeStatus(Guid id, TrangThaiXe status)
    {
        var result = await _vehicleService.ChangeStatusByOwnerAsync(id, GetCurrentUserId(), status);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Success
            ? "Đã cập nhật trạng thái xe."
            : result.Error;
        return RedirectToAction(nameof(MyVehicles));
    }

    private Guid GetCurrentUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
