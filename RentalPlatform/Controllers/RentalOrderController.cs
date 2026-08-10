using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalPlatform.Models.Entities;
using RentalPlatform.Models.ViewModels.RentalOrder;
using RentalPlatform.Services;

namespace RentalPlatform.Controllers;

[Authorize]
public class RentalOrderController : Controller
{
    private readonly IRentalOrderService _orders;
    private readonly IRentalContractWordService _contractWord;

    public RentalOrderController(IRentalOrderService orders, IRentalContractWordService contractWord)
    {
        _orders = orders;
        _contractWord = contractWord;
    }

    [Authorize(Roles = "Customer")]
    [HttpGet]
    public async Task<IActionResult> Checkout()
    {
        var model = await _orders.BuildCheckoutAsync(CurrentUserId);
        if (model.Items.Count == 0)
        {
            TempData["ErrorMessage"] = "Giỏ hàng đang trống.";
            return RedirectToAction("Index", "Cart");
        }
        return View(model);
    }

    [Authorize(Roles = "Customer")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(CheckoutViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var result = await _orders.CreateAsync(model, CurrentUserId);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View(model);
        }
        TempData["SuccessMessage"] = "Đã tạo đơn thuê nhiều xe. Đơn đang chờ chủ xe xác nhận.";
        return RedirectToAction(nameof(Details), new { id = result.Order!.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id)
    {
        var order = await _orders.GetDetailAsync(id, CurrentUserId, User.IsInRole("Admin"));
        return order is null ? NotFound() : View(order);
    }

    [HttpGet]
    public async Task<IActionResult> DownloadContract(Guid id)
    {
        var order = await _orders.GetDetailAsync(id, CurrentUserId, User.IsInRole("Admin"));
        if (order?.Contract is null) return NotFound();

        var document = _contractWord.Generate(order);
        return File(
            document.Content,
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            document.FileName);
    }

    [Authorize(Roles = "Customer")]
    [HttpGet]
    public async Task<IActionResult> MyOrders() => View(await _orders.GetCustomerOrdersAsync(CurrentUserId));

    [Authorize(Roles = "Owner")]
    [HttpGet]
    public async Task<IActionResult> OwnerOrders() => View(await _orders.GetOwnerOrdersAsync(CurrentUserId));

    [Authorize(Roles = "Owner")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(Guid id) => RedirectWithResult(await _orders.ConfirmAsync(id, CurrentUserId), id, "Đã xác nhận đơn.");

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(Guid id, string? reason) => RedirectWithResult(await _orders.CancelAsync(id, CurrentUserId, reason), id, "Đã huỷ đơn.");

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Sign(Guid id) => RedirectWithResult(await _orders.SignContractAsync(id, CurrentUserId), id, "Đã ký hợp đồng điện tử.");

    [Authorize(Roles = "Owner")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmCash(Guid id) => RedirectWithResult(await _orders.ConfirmCashAsync(id, CurrentUserId), id, "Đã xác nhận thu tiền.");

    [Authorize(Roles = "Owner")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> HandOver(Guid id) => RedirectWithResult(await _orders.HandOverAsync(id, CurrentUserId), id, "Đã xác nhận giao toàn bộ xe.");

    [Authorize(Roles = "Owner")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(Guid id) => RedirectWithResult(await _orders.CompleteAsync(id, CurrentUserId), id, "Đã nhận lại xe và hoàn thành đơn.");

    private IActionResult RedirectWithResult(RentalOrderOperationResult result, Guid id, string successMessage)
    {
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Success ? successMessage : result.Error;
        return RedirectToAction(nameof(Details), new { id });
    }

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
