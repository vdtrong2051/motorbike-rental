using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalPlatform.Models.Entities;
using RentalPlatform.Models.ViewModels.Vehicle;
using RentalPlatform.Services;

namespace RentalPlatform.Controllers;

public class VehicleController : Controller
{
    private readonly IVehicleService _vehicleService;

    public VehicleController(IVehicleService vehicleService)
    {
        _vehicleService = vehicleService;
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
        return View(vehicle);
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
