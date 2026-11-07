using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RentalPlatform.Data;
using RentalPlatform.Models.Entities;
using RentalPlatform.Services.Payments;

// Test fixture generator. Run ONLY with a disposable CI database; never against live data.
var connection = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
    ?? throw new InvalidOperationException("Missing disposable CI database connection.");
var path = Environment.GetEnvironmentVariable("GD2_FIXTURE_PATH")
    ?? throw new InvalidOperationException("GD2_FIXTURE_PATH required for disposable tests.");
if (!string.Equals(Environment.GetEnvironmentVariable("GD2_TEST_DATABASE"), "I_UNDERSTAND_DISPOSABLE", StringComparison.Ordinal))
    throw new InvalidOperationException("Guard: run only with GD2_TEST_DATABASE=I_UNDERSTAND_DISPOSABLE.");
var opts = new DbContextOptionsBuilder<AppDbContext>().UseMySQL(connection).Options;
var cfg = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
{
    ["Payment:PendingMinutes"] = "30", ["Payment:DemoEnabled"] = "true"
}).Build();
AppDbContext Open() => new(opts);
var suffix = Guid.NewGuid().ToString("N")[..12];
const string password = "GD2_Test_Password#2026";
User customer = new() { Email = $"gd2-c-{suffix}@example.test", HoTen = "GD2 Customer", Role = UserRole.Customer };
User other = new() { Email = $"gd2-x-{suffix}@example.test", HoTen = "GD2 Other", Role = UserRole.Customer };
User owner = new() { Email = $"gd2-o-{suffix}@example.test", HoTen = "GD2 Owner", Role = UserRole.Owner };
var hasher = new PasswordHasher<User>();
foreach (var user in new[] { customer, other, owner }) user.PasswordHash = hasher.HashPassword(user, password);
await using (var db = Open())
{
    db.Users.AddRange(customer, other, owner);
    await db.SaveChangesAsync();
}
async Task<Guid> Create(PhuongThucThanhToanDon method)
{
    await using var db = Open();
    var id = Guid.NewGuid();
    var order = new RentalOrder
    {
        Id = id, MaDon = "GD2-" + Guid.NewGuid().ToString("N")[..12],
        CustomerId = customer.Id, OwnerId = owner.Id,
        HoTenKhach = customer.HoTen, EmailKhach = customer.Email, SDTKhach = "0901234567",
        SoCCCD = "123456789012", SoGPLX = "123456789", DiaChiKhach = "GD2 CI",
        TongTien = 240000m, TienThue = 190000m, TienDatCoc = 40000m, PhiDichVu = 10000m,
        TrangThai = TrangThaiDonThue.DaXacNhan,
        Contract = new RentalOrderContract
        {
            SoHopDong = "GD2-HD-" + Guid.NewGuid().ToString("N")[..10],
            NoiDungHopDong = "Disposable CI fixture, no real money",
            HoTenChuXe = owner.HoTen, EmailChuXe = owner.Email,
            DaKyKhach = true, DaKyChuXe = true
        }
    };
    db.RentalOrders.Add(order);
    await db.SaveChangesAsync();
    var result = await new PaymentCore(db, cfg).SelectAsync(id, method, customer.Id);
    if (!result.Success) throw new InvalidOperationException("Could not create GD2 fixture: " + result.Error);
    return id;
}
var paid = await Create(PhuongThucThanhToanDon.ChuyenKhoan);
var failed = await Create(PhuongThucThanhToanDon.ViDienTu);
var cancelled = await Create(PhuongThucThanhToanDon.TheThanhToan);
var expired = await Create(PhuongThucThanhToanDon.ChuyenKhoan);
await using (var db = Open())
{
    var payment = await db.RentalOrderPayments.SingleAsync(p => p.RentalOrderId == expired);
    payment.CreatedAt = DateTime.UtcNow.AddMinutes(-45);
    await db.SaveChangesAsync();
}
var fixture = new
{
    email = customer.Email, otherEmail = other.Email, password,
    paid = paid.ToString(), failed = failed.ToString(),
    cancelled = cancelled.ToString(), expired = expired.ToString()
};
await File.WriteAllTextAsync(path, JsonSerializer.Serialize(fixture));
Console.WriteLine("GD2 fixtures created in disposable MySQL; JSON written to specified local CI file.");
