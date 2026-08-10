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

        await SignInUserAsync(user, model.RememberMe);

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
                model.TrangThaiXacThuc = current.TrangThaiXacThuc;
                model.GhiChuXacThuc = current.GhiChuXacThuc;
                model.HoSoDayDu = current.HoSoDayDu;
                model.CoAnhCCCDMatTruoc = current.CoAnhCCCDMatTruoc;
                model.CoAnhCCCDMatSau = current.CoAnhCCCDMatSau;
            }
            return View(model);
        }

        var (success, error) = await _accountService.UpdateProfileAsync(CurrentUserId, model);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] = success
            ? "Đã cập nhật hồ sơ dùng cho hợp đồng."
            : error;
        return RedirectToAction(nameof(Profile));
    }

    [HttpGet]
    public IActionResult GoogleLogin(string? returnUrl = null)
    {
        if (string.IsNullOrWhiteSpace(HttpContext.RequestServices.GetRequiredService<IConfiguration>()["Authentication:Google:ClientId"]))
            return RedirectToAction(nameof(Login));
        var redirect = Url.Action(nameof(GoogleCallback), new { returnUrl });
        return Challenge(new AuthenticationProperties { RedirectUri = redirect }, "Google");
    }

    [HttpGet]
    public async Task<IActionResult> GoogleCallback(string? returnUrl = null)
    {
        var result = await HttpContext.AuthenticateAsync("External");
        if (!result.Succeeded || result.Principal is null)
        {
            TempData["ErrorMessage"] = "Đăng nhập Google không thành công.";
            return RedirectToAction(nameof(Login));
        }
        var subject = result.Principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = result.Principal.FindFirstValue(ClaimTypes.Email);
        var name = result.Principal.FindFirstValue(ClaimTypes.Name) ?? email;
        var verified = result.Principal.FindFirstValue("google_email_verified");
        if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(email) || verified != "true")
            return RedirectToAction(nameof(Login));

        var user = await _accountService.FindOrCreateExternalUserAsync("Google", subject, email, name!);
        await HttpContext.SignOutAsync("External");
        if (user.DangBiTamKhoa) return RedirectToAction(nameof(AccessDenied));
        await SignInUserAsync(user, true);
        return !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? Redirect(returnUrl) : RedirectToAction("Index", "Home");
    }

    [Authorize, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitVerification()
    {
        var (success, error) = await _accountService.SubmitVerificationAsync(CurrentUserId);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] = success
            ? "Hồ sơ đã được gửi cho Admin xác thực." : error;
        return RedirectToAction(nameof(Profile));
    }

    [Authorize, HttpGet]
    public async Task<IActionResult> IdentityDocument(string side)
    {
        if (side is not ("front" or "back")) return BadRequest();
        var document = await _accountService.GetIdentityDocumentAsync(CurrentUserId, side);
        if (document is null) return NotFound();
        Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
        Response.Headers.Pragma = "no-cache";
        Response.Headers.XContentTypeOptions = "nosniff";
        return File(document.Bytes, document.ContentType);
    }

    public IActionResult AccessDenied() => View();

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private async Task SignInUserAsync(RentalPlatform.Models.Entities.User user, bool persistent)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()), new(ClaimTypes.Name, user.HoTen),
            new(ClaimTypes.Email, user.Email), new(ClaimTypes.Role, user.Role.ToString())
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
            new AuthenticationProperties { IsPersistent = persistent });
    }
}
