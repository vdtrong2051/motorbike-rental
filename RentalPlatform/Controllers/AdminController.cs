using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalPlatform.Services;

namespace RentalPlatform.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly IVehicleService _vehicleService;
    private readonly IDashboardService _dashboardService;

    public AdminController(IVehicleService vehicleService, IDashboardService dashboardService)
    {
        _vehicleService = vehicleService;
        _dashboardService = dashboardService;
    }

    // GET /Admin/Dashboard — thống kê doanh thu & tỷ lệ sử dụng xe toàn hệ thống (Sprint 5)
    [HttpGet]
    public async Task<IActionResult> Dashboard()
    {
        var vm = await _dashboardService.GetAdminDashboardAsync();
        return View(vm);
    }

    // GET /Admin/PendingVehicles — danh sách xe đang chờ duyệt (mới đăng bởi Owner)
    [HttpGet]
    public async Task<IActionResult> PendingVehicles()
    {
        var vehicles = await _vehicleService.GetPendingApprovalAsync();
        return View(vehicles);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveVehicle(Guid id)
    {
        var result = await _vehicleService.ApproveAsync(id);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Success
            ? "Đã duyệt xe. Xe hiện đã ở trạng thái Sẵn sàng cho thuê."
            : result.Error;
        return RedirectToAction(nameof(PendingVehicles));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectVehicle(Guid id, string lyDo)
    {
        var result = await _vehicleService.RejectAsync(id, lyDo);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Success
            ? "Đã từ chối xe."
            : result.Error;
        return RedirectToAction(nameof(PendingVehicles));
    }
}