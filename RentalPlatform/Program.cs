using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RentalPlatform.Data;
using RentalPlatform.Models.Entities;
using RentalPlatform.Repositories;
using RentalPlatform.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();
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

// Tăng giới hạn dung lượng request để cho phép upload nhiều ảnh xe (mặc định Kestrel ~28.6MB)
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 50 * 1024 * 1024; // 50MB
});
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 50 * 1024 * 1024; // 50MB
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// ----- Seed 1 tài khoản Admin nếu DB chưa có tài khoản Admin nào -----
// Chỉ chạy 1 lần: nếu đã tồn tại user Role = Admin thì bỏ qua, không tạo trùng.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var daCoAdmin = await db.Users.AnyAsync(u => u.Role == UserRole.Admin);
    if (!daCoAdmin)
    {
        var hasher = new PasswordHasher<User>();
        var admin = new User
        {
            HoTen = "Quản trị viên",
            Email = "admin@rentalplatform.vn",
            Role = UserRole.Admin
        };
        admin.PasswordHash = hasher.HashPassword(admin, "Admin@123");

        db.Users.Add(admin);
        await db.SaveChangesAsync();

        Console.WriteLine("[Seed] Đã tạo tài khoản Admin mặc định: admin@rentalplatform.vn / Admin@123");
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
