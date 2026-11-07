using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using RentalPlatform.Data;
using RentalPlatform.Models.Entities;
using RentalPlatform.Services;
using RentalPlatform.Services.Payments;

var connection = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
    ?? throw new Exception("Set ConnectionStrings__DefaultConnection for disposable MySQL tests.");
var options = new DbContextOptionsBuilder<AppDbContext>().UseMySQL(connection).Options;
var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
{
    ["Payment:PendingMinutes"] = "30",
    ["Payment:DemoEnabled"] = "true"
}).Build();
var pass = 0;
void Check(bool condition, string label)
{
    if (!condition) throw new Exception("GD1 FAIL: " + label);
    Console.WriteLine("PASS " + label);
    pass++;
}
AppDbContext Open() => new(options);
PaymentCore Core(AppDbContext db) => new(db, config);

// All fixtures are isolated by random emails; CI database is disposable.
var suffix = Guid.NewGuid().ToString("N")[..12];
Guid customerId;
Guid ownerId;
await using (var db = Open())
{
    var customer = new User { HoTen = "GD1 Customer", Email = $"gd1c-{suffix}@example.test", Role = UserRole.Customer, PasswordHash = "not-used" };
    var owner = new User { HoTen = "GD1 Owner", Email = $"gd1o-{suffix}@example.test", Role = UserRole.Owner, PasswordHash = "not-used" };
    db.Users.AddRange(customer, owner);
    await db.SaveChangesAsync();
    customerId = customer.Id;
    ownerId = owner.Id;
}

async Task<Guid> NewOrder()
{
    await using var db = Open();
    var order = new RentalOrder
    {
        MaDon = "GD1-" + Guid.NewGuid().ToString("N")[..12],
        CustomerId = customerId,
        OwnerId = ownerId,
        HoTenKhach = "GD1 Customer",
        EmailKhach = $"gd1c-{suffix}@example.test",
        SDTKhach = "0901234567",
        SoCCCD = "123456789012",
        SoGPLX = "123456789",
        DiaChiKhach = "GD1 Test",
        TongTien = 150000m,
        TienThue = 120000m,
        TienDatCoc = 20000m,
        PhiDichVu = 10000m,
        TrangThai = TrangThaiDonThue.DaXacNhan,
        Contract = new RentalOrderContract
        {
            SoHopDong = "GD1-HD-" + Guid.NewGuid().ToString("N")[..11],
            NoiDungHopDong = "GD1 Test Contract",
            HoTenChuXe = "GD1 Owner",
            EmailChuXe = $"gd1o-{suffix}@example.test",
            DaKyKhach = true,
            DaKyChuXe = true
        }
    };
    db.RentalOrders.Add(order);
    await db.SaveChangesAsync();
    return order.Id;
}

async Task<(string Reference, int Count)> Select(Guid orderId, PhuongThucThanhToanDon method)
{
    await using var db = Open();
    var result = await Core(db).SelectAsync(orderId, method, customerId);
    Check(result.Success, "Select " + method);
    var count = await db.RentalOrderPayments.CountAsync(p => p.RentalOrderId == orderId);
    return (result.Order!.MaThamChieuThanhToan!, count);
}

// Stable selection must NOT create duplicate attempts.
var paidOrder = await NewOrder();
var (firstRef, firstCount) = await Select(paidOrder, PhuongThucThanhToanDon.ChuyenKhoan);
var (sameRef, sameCount) = await Select(paidOrder, PhuongThucThanhToanDon.ChuyenKhoan);
Check(firstRef == sameRef && firstCount == 1 && sameCount == 1, "Same-method retry is idempotent");
await using (var db = Open())
{
    var core = Core(db);
    Check(!(await core.ConfirmAsync(paidOrder, firstRef, 100m, PaymentSignalSource.Webhook)).Success, "Reject wrong amount");
    Check(!(await core.ConfirmAsync(paidOrder, "PAY-invalid", 150000m, PaymentSignalSource.Webhook)).Success, "Reject wrong reference");
    Check((await core.ConfirmAsync(paidOrder, firstRef, 150000m, PaymentSignalSource.Webhook)).Success, "Webhook confirms valid payment");
}
await using (var db = Open())
{
    Check((await Core(db).ConfirmAsync(paidOrder, firstRef, 150000m, PaymentSignalSource.Webhook)).Success, "Paid callback replay is idempotent");
    Check(await db.RentalOrderPayments.CountAsync(p => p.RentalOrderId == paidOrder && p.TrangThai == TrangThaiThanhToanDon.DaThanhToan) == 1,
        "Exactly one paid attempt");
}

var switched = await NewOrder();
var (oldRef, _) = await Select(switched, PhuongThucThanhToanDon.ChuyenKhoan);
var (newRef, _) = await Select(switched, PhuongThucThanhToanDon.ViDienTu);
Check(oldRef != newRef, "New reference after switching method");
await using (var db = Open())
{
    var core = Core(db);
    Check(!(await core.ConfirmAsync(switched, oldRef, 150000m, PaymentSignalSource.Webhook)).Success, "Reject replaced callback");
    Check((await core.FailAsync(switched, newRef, null, PaymentSignalSource.Demo, "test decline")).Success,
        "Demo failure accepted");
}
var (retryRef, _) = await Select(switched, PhuongThucThanhToanDon.TheThanhToan);
Check(retryRef != newRef, "Retry after failed payment");
await using (var db = Open())
    Check((await Core(db).ConfirmAsync(switched, retryRef, 150000m, PaymentSignalSource.Demo)).Success, "Demo card confirmation");

var cancelled = await NewOrder();
var (cancelRef, _) = await Select(cancelled, PhuongThucThanhToanDon.ChuyenKhoan);
await using (var db = Open())
{
    // Constructor's cart is not accessed by CancelAsync. Exercise the real order cancellation path.
    var service = new RentalOrderService(db, null!, Core(db));
    Check((await service.CancelAsync(cancelled, customerId, "customer changed mind")).Success, "Cancel pending order");
}
await using (var db = Open())
{
    Check(!(await Core(db).ConfirmAsync(cancelled, cancelRef, 150000m, PaymentSignalSource.Webhook)).Success,
        "Reject late callback after cancellation");
    var payment = await db.RentalOrderPayments.SingleAsync(p => p.MaGiaoDich == cancelRef);
    Check(payment.TrangThai == TrangThaiThanhToanDon.ThatBai, "Cancellation invalidates pending payment");
}

var expired = await NewOrder();
var (expiredRef, _) = await Select(expired, PhuongThucThanhToanDon.ViDienTu);
await using (var db = Open())
{
    var payment = await db.RentalOrderPayments.SingleAsync(p => p.MaGiaoDich == expiredRef);
    payment.CreatedAt = DateTime.UtcNow.AddMinutes(-45);
    await db.SaveChangesAsync();
}
await using (var db = Open())
{
    Check(!(await Core(db).ConfirmAsync(expired, expiredRef, 150000m, PaymentSignalSource.Webhook)).Success,
        "Expired payment rejected");
    Check((await db.RentalOrderPayments.SingleAsync(p => p.MaGiaoDich == expiredRef)).TrangThai == TrangThaiThanhToanDon.ThatBai,
        "Expired attempt marked failed");
}

var cash = await NewOrder();
var (cashRef, _) = await Select(cash, PhuongThucThanhToanDon.ThanhToanKhiNhanXe);
await using (var db = Open())
{
    var core = Core(db);
    Check(!(await core.ConfirmAsync(cash, cashRef, 150000m, PaymentSignalSource.Webhook)).Success,
        "Reject cash via webhook");
    Check(!(await core.ConfirmAsync(cash, cashRef, 150000m, PaymentSignalSource.OwnerCash, ownerId: customerId)).Success,
        "Reject cash confirmation by non-owner");
    Check((await core.ConfirmAsync(cash, cashRef, 150000m, PaymentSignalSource.OwnerCash, ownerId: ownerId)).Success,
        "Allow correct owner cash confirmation");
}

var racing = await NewOrder();
var (raceRef, _) = await Select(racing, PhuongThucThanhToanDon.ChuyenKhoan);
async Task<bool> ConcurrentConfirmation()
{
    await using var db = Open();
    return (await Core(db).ConfirmAsync(racing, raceRef, 150000m, PaymentSignalSource.Webhook)).Success;
}
var results = await Task.WhenAll(ConcurrentConfirmation(), ConcurrentConfirmation());
Check(results.All(x => x), "Two concurrent duplicate callbacks handled");
await using (var db = Open())
    Check(await db.RentalOrderPayments.CountAsync(p => p.RentalOrderId == racing && p.TrangThai == TrangThaiThanhToanDon.DaThanhToan) == 1,
        "Concurrent callback did not create duplicate revenue");

var protection = DataProtectionProvider.Create("MotorbikeRental.GD1.Integration");
var tokens = new DemoPaymentTokenService(protection, config, new TestHostEnvironment());
var demoToken = tokens.Issue(racing, raceRef, DateTime.UtcNow.AddMinutes(10));
Check(tokens.TryRead(demoToken, out var tokenOrderId, out var tokenReference)
      && tokenOrderId == racing && tokenReference == raceRef, "Signed demo token round trip");
Check(!tokens.TryRead(demoToken + "tampered", out _, out _), "Reject forged/tampered token");

Console.WriteLine($"GD1 CORE PASS: {pass} checks. No schema changes or new migration.");

sealed class TestHostEnvironment : IHostEnvironment
{
    public string EnvironmentName { get; set; } = Environments.Development;
    public string ApplicationName { get; set; } = "GD1Test";
    public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}
