using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalPlatform.Data;
using RentalPlatform.Models.Entities;
using RentalPlatform.Services;

namespace RentalPlatform.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly IVehicleService _vehicleService;
    private readonly IDashboardService _dashboardService;
    private readonly AppDbContext _db;
    private readonly IAccountService _accountService;

    public AdminController(IVehicleService vehicleService, IDashboardService dashboardService, AppDbContext db, IAccountService accountService)
    {
        _vehicleService = vehicleService;
        _dashboardService = dashboardService;
        _db = db;
        _accountService = accountService;
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

    [HttpGet]
    public async Task<IActionResult> Vehicles() => View(await _db.Vehicles.AsNoTracking().Include(v => v.Owner)
        .OrderByDescending(v => v.CreatedAt).ToListAsync());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetVehicleStatus(Guid id, TrangThaiXe status, string? note)
    {
        var vehicle = await _db.Vehicles.FindAsync(id);
        if (vehicle is not null)
        {
            vehicle.TrangThai = status; vehicle.LyDoTuChoi = status == TrangThaiXe.TuChoi ? note : null;
            vehicle.ApprovedAt = status == TrangThaiXe.SanSang ? DateTime.UtcNow : vehicle.ApprovedAt;
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Vehicles));
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

    [HttpGet]
    public async Task<IActionResult> Users() => View(await _db.Users.AsNoTracking().OrderByDescending(u => u.CreatedAt).ToListAsync());

    [HttpGet]
    public async Task<IActionResult> IdentityDocument(Guid id, string side)
    {
        if (side is not ("front" or "back")) return BadRequest();
        var document = await _accountService.GetIdentityDocumentAsync(id, side);
        if (document is null) return NotFound();
        Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
        Response.Headers.Pragma = "no-cache";
        Response.Headers.XContentTypeOptions = "nosniff";
        return File(document.Bytes, document.ContentType);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyUser(Guid id, bool approve, string? note)
    {
        var user = await _db.Users.FindAsync(id);
        if (user is not null && user.Role != UserRole.Admin)
        {
            user.TrangThaiXacThuc = approve ? TrangThaiXacThucNguoiDung.DaXacThuc : TrangThaiXacThucNguoiDung.TuChoi;
            user.GhiChuXacThuc = note;
            user.XacThucAt = approve ? DateTime.UtcNow : null;
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Users));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> LockUser(Guid id, int days = 0)
    {
        var user = await _db.Users.FindAsync(id);
        if (user is not null && user.Role != UserRole.Admin)
        {
            user.BiTamKhoa = true;
            user.TamKhoaDen = days > 0 ? DateTime.UtcNow.AddDays(days) : null;
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Users));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UnlockUser(Guid id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user is not null) { user.BiTamKhoa = false; user.TamKhoaDen = null; await _db.SaveChangesAsync(); }
        return RedirectToAction(nameof(Users));
    }

    [HttpGet]
    public async Task<IActionResult> Policies() => View(await GetPolicyAsync());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Policies(SystemPolicy model)
    {
        if (!ModelState.IsValid) return View(model);
        var policy = await GetPolicyAsync();
        policy.TyLeDatCoc = model.TyLeDatCoc; policy.TyLePhiDichVu = model.TyLePhiDichVu;
        policy.HoanTienTruoc72Gio = model.HoanTienTruoc72Gio; policy.HoanTienTruoc24Gio = model.HoanTienTruoc24Gio;
        policy.HoanTienTrong24Gio = model.HoanTienTrong24Gio; policy.PhatTraMuonMoiGio = model.PhatTraMuonMoiGio;
        policy.QuyDinhViPham = model.QuyDinhViPham; policy.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(); TempData["SuccessMessage"] = "Đã cập nhật chính sách hệ thống.";
        return RedirectToAction(nameof(Policies));
    }

    [HttpGet]
    public async Task<IActionResult> Violations() => View(await _db.ViolationReports.AsNoTracking()
        .Include(r => r.Reporter).Include(r => r.ReportedUser).Include(r => r.RentalOrder)
        .OrderByDescending(r => r.CreatedAt).ToListAsync());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ResolveViolation(Guid id, HinhThucXuLyViPham action, string? note, bool reject = false)
    {
        var report = await _db.ViolationReports.Include(r => r.ReportedUser).FirstOrDefaultAsync(r => r.Id == id);
        if (report is null) return NotFound();
        report.TrangThai = reject ? TrangThaiBaoCaoViPham.TuChoi : TrangThaiBaoCaoViPham.DaXuLy;
        report.HinhThucXuLy = reject ? HinhThucXuLyViPham.KhongXuLy : action;
        report.GhiChuAdmin = note; report.ResolvedAt = DateTime.UtcNow;
        if (!reject && action != HinhThucXuLyViPham.KhongXuLy)
        {
            report.ReportedUser.SoLanViPham++;
            if (action is HinhThucXuLyViPham.Khoa7Ngay or HinhThucXuLyViPham.Khoa30Ngay or HinhThucXuLyViPham.KhoaVoThoiHan)
            {
                report.ReportedUser.BiTamKhoa = true;
                report.ReportedUser.TamKhoaDen = action == HinhThucXuLyViPham.Khoa7Ngay ? DateTime.UtcNow.AddDays(7)
                    : action == HinhThucXuLyViPham.Khoa30Ngay ? DateTime.UtcNow.AddDays(30) : null;
            }
        }
        await _db.SaveChangesAsync(); return RedirectToAction(nameof(Violations));
    }

    [HttpGet]
    public async Task<IActionResult> Payments() => View(await _db.RentalOrderPayments.AsNoTracking()
        .Include(p => p.RentalOrder).ThenInclude(o => o.Customer).OrderByDescending(p => p.CreatedAt).ToListAsync());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ReconcilePayment(Guid id, string? note)
    {
        var payment = await _db.RentalOrderPayments.FindAsync(id);
        if (payment is not null) { payment.ReconciledAt = DateTime.UtcNow; payment.GhiChuDoiSoat = note; await _db.SaveChangesAsync(); }
        return RedirectToAction(nameof(Payments));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmRefund(Guid id, string? note)
    {
        var payment = await _db.RentalOrderPayments.Include(p => p.RentalOrder).FirstOrDefaultAsync(p => p.Id == id);
        if (payment is not null && payment.RentalOrder.TrangThaiThanhToan == TrangThaiThanhToanDon.ChoHoanTien)
        {
            payment.TrangThai = TrangThaiThanhToanDon.DaHoanTien;
            payment.ReconciledAt = DateTime.UtcNow; payment.GhiChuDoiSoat = note;
            payment.RentalOrder.TrangThaiThanhToan = TrangThaiThanhToanDon.DaHoanTien;
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Payments));
    }

    private async Task<SystemPolicy> GetPolicyAsync()
    {
        var policy = await _db.SystemPolicies.FirstOrDefaultAsync(p => p.Id == 1);
        if (policy is null) { policy = new SystemPolicy(); _db.SystemPolicies.Add(policy); await _db.SaveChangesAsync(); }
        return policy;
    }
}
