using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalPlatform.Models.ViewModels.Account;
using RentalPlatform.Services;

namespace RentalPlatform.Controllers;

public class AccountController : Controller
{
    private readonly IAccountService _accountService;

    public AccountController(IAccountService accountService)
    {
        _accountService = accountService;
    }

    [HttpGet]
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var (success, error) = await _accountService.RegisterAsync(model);
        if (!success)
        {
            ModelState.AddModelError(nameof(model.Email), error!);
            return View(model);
        }

        TempData["SuccessMessage"] = "Đăng ký thành công, vui lòng đăng nhập.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null) => View(new LoginViewModel { ReturnUrl = returnUrl });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _accountService.ValidateLoginAsync(model.Email, model.Password);
        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không đúng.");
            return View(model);
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.HoTen),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString())
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties { IsPersistent = model.RememberMe });

        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            return Redirect(model.ReturnUrl);

        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var model = await _accountService.GetProfileAsync(CurrentUserId);
        return model is null ? NotFound() : View(model);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileViewModel model)
    {
        ModelState.Remove(nameof(model.Email));
        ModelState.Remove(nameof(model.Role));
        if (!ModelState.IsValid)
        {
            var current = await _accountService.GetProfileAsync(CurrentUserId);
            if (current is not null)
            {
                model.Email = current.Email;
                model.Role = current.Role;
            }
            return View(model);
        }

        var (success, error) = await _accountService.UpdateProfileAsync(CurrentUserId, model);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] = success
            ? "Đã cập nhật hồ sơ dùng cho hợp đồng."
            : error;
        return RedirectToAction(nameof(Profile));
    }

    public IActionResult AccessDenied() => View();

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
