using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Net;
using RentalPlatform.Data;
using RentalPlatform.Models.Entities;
using RentalPlatform.Repositories;
using RentalPlatform.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

    // Nginx và Kestrel chạy trên cùng máy. Chỉ tin cậy reverse proxy loopback,
    // không chấp nhận các header X-Forwarded-* giả từ Internet.
    options.KnownProxies.Add(IPAddress.Loopback);
});
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".RentalPlatform.Cart";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromDays(7);
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySQL(builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Thiếu connection string DefaultConnection cho MySQL.")));

builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddSingleton<IIdentityDocumentStorage, IdentityDocumentStorage>();
builder.Services.AddScoped<IVehicleRepository, VehicleRepository>();
builder.Services.AddScoped<IVehicleService, VehicleService>();
builder.Services.AddScoped<IBookingRepository, BookingRepository>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IContractRepository, ContractRepository>();
builder.Services.AddScoped<IContractService, ContractService>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IDashboardService, DashboardService>(); // TODO: xác nhận đúng tên class
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IRentalOrderService, RentalOrderService>();
builder.Services.AddSingleton<IRentalContractWordService, RentalContractWordService>();

// Tăng giới hạn dung lượng request để cho phép upload nhiều ảnh xe (mặc định Kestrel ~28.6MB)
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 50 * 1024 * 1024; // 50MB
});
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 50 * 1024 * 1024; // 50MB
});

var authentication = builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
        options.Events.OnValidatePrincipal = async context =>
        {
            var rawId = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(rawId, out var userId)) return;
            var authDb = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
            var state = await authDb.Users.AsNoTracking().Where(u => u.Id == userId)
                .Select(u => new { u.BiTamKhoa, u.TamKhoaDen }).FirstOrDefaultAsync();
            if (state is null || (state.BiTamKhoa && (!state.TamKhoaDen.HasValue || state.TamKhoaDen > DateTime.UtcNow)))
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
        };
    })
    .AddCookie("External", options => options.ExpireTimeSpan = TimeSpan.FromMinutes(10));

var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
{
    authentication.AddOAuth("Google", options =>
    {
        options.ClientId = googleClientId;
        options.ClientSecret = googleClientSecret;
        options.SignInScheme = "External";
        options.SaveTokens = true;
        options.AuthorizationEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";
        options.TokenEndpoint = "https://oauth2.googleapis.com/token";
        options.UserInformationEndpoint = "https://openidconnect.googleapis.com/v1/userinfo";
        options.CallbackPath = "/signin-google";
        options.Scope.Add("openid"); options.Scope.Add("profile"); options.Scope.Add("email");
        options.Events = new OAuthEvents
        {
            OnCreatingTicket = async context =>
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, context.Options.UserInformationEndpoint);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", context.AccessToken);
                using var response = await context.Backchannel.SendAsync(request, context.HttpContext.RequestAborted);
                response.EnsureSuccessStatusCode();
                using var user = JsonDocument.Parse(await response.Content.ReadAsStringAsync(context.HttpContext.RequestAborted));
                var root = user.RootElement;
                context.Identity?.AddClaim(new Claim(ClaimTypes.NameIdentifier, root.GetProperty("sub").GetString()!));
                context.Identity?.AddClaim(new Claim(ClaimTypes.Name, root.GetProperty("name").GetString()!));
                context.Identity?.AddClaim(new Claim(ClaimTypes.Email, root.GetProperty("email").GetString()!));
                context.Identity?.AddClaim(new Claim("google_email_verified", root.TryGetProperty("email_verified", out var verified) && verified.GetBoolean() ? "true" : "false"));
            }
        };
    });
}

builder.Services.AddAuthorization();

var app = builder.Build();

// Phải chạy trước HTTPS redirection/HSTS để ASP.NET Core nhận biết request gốc
// đã dùng HTTPS tại Nginx và tránh vòng lặp chuyển hướng.
app.UseForwardedHeaders();

// ----- Seed 1 tài khoản Admin nếu DB chưa có tài khoản Admin nào -----
// Chỉ chạy 1 lần: nếu đã tồn tại user Role = Admin thì bỏ qua, không tạo trùng.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var daCoAdmin = await db.Users.AnyAsync(u => u.Role == UserRole.Admin);
    if (!daCoAdmin)
    {
        var adminEmail = builder.Configuration["SeedAdmin:Email"];
        var adminPassword = builder.Configuration["SeedAdmin:Password"];
        var adminName = builder.Configuration["SeedAdmin:Name"] ?? "Quản trị viên";

        if (app.Environment.IsDevelopment())
        {
            adminEmail ??= "admin@rentalplatform.vn";
            adminPassword ??= "Admin@123";
        }

        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
        {
            throw new InvalidOperationException(
                "Database chưa có Admin. Hãy cấu hình SeedAdmin__Email và " +
                "SeedAdmin__Password bằng biến môi trường trước khi chạy Production.");
        }

        var hasher = new PasswordHasher<User>();
        var admin = new User
        {
            HoTen = adminName,
            Email = adminEmail,
            Role = UserRole.Admin
        };
        admin.PasswordHash = hasher.HashPassword(admin, adminPassword);

        db.Users.Add(admin);
        await db.SaveChangesAsync();

        Console.WriteLine($"[Seed] Đã tạo tài khoản Admin: {adminEmail}");
    }

    if (!await db.SystemPolicies.AnyAsync())
    {
        db.SystemPolicies.Add(new SystemPolicy());
        await db.SaveChangesAsync();
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
