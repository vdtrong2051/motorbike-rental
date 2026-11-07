using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RentalPlatform.Data;
using RentalPlatform.Models.Entities;
using RentalPlatform.Models.ViewModels.Review;
using RentalPlatform.Services;

// ALL commands operate on an isolated, explicitly opted-in MySQL test database.
if (Environment.GetEnvironmentVariable("GD3_TEST_DATABASE") != "I_UNDERSTAND_DISPOSABLE")
    throw new InvalidOperationException("Refusing to run GD3 tests without GD3_TEST_DATABASE=I_UNDERSTAND_DISPOSABLE.");
var connection = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
    ?? throw new InvalidOperationException("Missing isolated MySQL connection string.");
if (!connection.Contains("127.0.0.1", StringComparison.OrdinalIgnoreCase)
    && !connection.Contains("localhost", StringComparison.OrdinalIgnoreCase))
    throw new InvalidOperationException("GD3 tests accept only localhost MySQL.");
var options = new DbContextOptionsBuilder<AppDbContext>().UseMySQL(connection).Options;
AppDbContext Open() => new(options);
IOrderReviewService Reviews(AppDbContext db) => new OrderReviewService(db);
void Check(bool pass, string name)
{
    if (!pass) throw new Exception("GD3 FAIL: " + name);
    Console.WriteLine("PASS " + name);
}

if (args.Length == 1 && args[0] == "init-schema")
{
    // The existing MySQL migration baseline must be initialized FIRST.
    var path = Path.Combine(Directory.GetCurrentDirectory(), "db", "gd3_order_item_reviews.sql");
    var ddl = await File.ReadAllTextAsync(path);
    await using var db = Open();
    await db.Database.ExecuteSqlRawAsync(ddl);
    Console.WriteLine("GD3 SQL table created/verified on disposable MySQL. No EF migration.");
    return;
}

static User MakeUser(string suffix, string role, string password)
{
    var u = new User
    {
        HoTen = "GD3 " + role,
        Email = $"gd3-{role.ToLowerInvariant()}-{suffix}@example.test",
        Role = Enum.Parse<UserRole>(role),
        TrangThaiXacThuc = TrangThaiXacThucNguoiDung.DaXacThuc
    };
    u.PasswordHash = new PasswordHasher<User>().HashPassword(u, password);
    return u;
}
static Vehicle MakeVehicle(Guid owner, string suffix, string name)
    => new()
    {
        OwnerId = owner, TenXe = name, LoaiXe = "Xe máy", BienSo = "GD3" + Guid.NewGuid().ToString("N")[..8],
        SoChoNgoi = 2, HopSo = HopSoXe.SoTuDong, NhienLieu = NhienLieuXe.Xang,
        Nam = 2025, KhuVuc = "Đà Nẵng", GiaThueMotNgay = 120000m, TrangThai = TrangThaiXe.SanSang
    };
static RentalOrder MakeOrder(Guid customerId, Guid ownerId, string email, TrangThaiDonThue status)
    => new()
    {
        MaDon = "GD3-" + Guid.NewGuid().ToString("N")[..12], CustomerId = customerId, OwnerId = ownerId,
        HoTenKhach = "GD3 Customer", EmailKhach = email, SDTKhach = "0901234567",
        SoCCCD = "123456789012", SoGPLX = "123456789", DiaChiKhach = "GD3 test address",
        TongTien = 360000m, TienThue = 320000m, TienDatCoc = 30000m, PhiDichVu = 10000m,
        TrangThai = status
    };
static RentalOrderItem MakeItem(Guid orderId, Vehicle vehicle)
    => new()
    {
        RentalOrderId = orderId, VehicleId = vehicle.Id, TenXe = vehicle.TenXe, BienSo = vehicle.BienSo,
        LoaiXe = vehicle.LoaiXe, TinhTrangLucDat = "GD3 test", NgayNhanXe = DateTime.UtcNow.AddDays(-4),
        NgayTraXe = DateTime.UtcNow.AddDays(-2), DonGiaNgay = 120000m, SoNgayThue = 2, ThanhTien = 240000m
    };

if (args.Length == 1 && args[0] == "seed-http")
{
    var suffix = Guid.NewGuid().ToString("N")[..10];
    const string pass = "GD3_Smoke_Password!2026";
    await using var db = Open();
    var customer = MakeUser(suffix, "Customer", pass);
    var other = MakeUser(suffix + "other", "Customer", pass);
    var owner = MakeUser(suffix, "Owner", pass);
    var admin = MakeUser(suffix, "Admin", pass);
    db.Users.AddRange(customer, other, owner, admin);
    var vehicle = MakeVehicle(owner.Id, suffix, "GD3 HTTP Demo Vehicle " + suffix);
    db.Vehicles.Add(vehicle);
    var order = MakeOrder(customer.Id, owner.Id, customer.Email, TrangThaiDonThue.HoanThanh);
    db.RentalOrders.Add(order);
    var itemPending = MakeItem(order.Id, vehicle);
    var itemModeration = MakeItem(order.Id, vehicle);
    db.RentalOrderItems.AddRange(itemPending, itemModeration);
    var review = new RentalOrderItemReview
    {
        RentalOrderItemId = itemModeration.Id, VehicleId = vehicle.Id, CustomerId = customer.Id,
        SoSao = 4, NhanXet = "GD3_COMMENT_FOR_MODERATION_" + suffix
    };
    db.RentalOrderItemReviews.Add(review);
    await db.SaveChangesAsync();
    var fixturePath = Environment.GetEnvironmentVariable("GD3_FIXTURE_PATH") ?? "/tmp/gd3-fixture.json";
    await File.WriteAllTextAsync(fixturePath, JsonSerializer.Serialize(new
    {
        email = customer.Email, otherEmail = other.Email, ownerEmail = owner.Email, adminEmail = admin.Email,
        password = pass, orderId = order.Id, itemId = itemPending.Id, moderationId = review.Id,
        vehicleId = vehicle.Id, comment = review.NhanXet
    }));
    Console.WriteLine("GD3 HTTP fixture created: " + fixturePath);
    return;
}

if (args.Length != 1 || args[0] != "core-tests")
    throw new InvalidOperationException("Use init-schema, core-tests or seed-http.");

var testSuffix = Guid.NewGuid().ToString("N")[..10];
Guid customerId, otherId, ownerId, adminId, firstItem, secondItem, thirdItem, racingItem, vehicleId, orderId;
await using (var db = Open())
{
    var customer = MakeUser(testSuffix, "Customer", "not-used");
    var other = MakeUser(testSuffix + "x", "Customer", "not-used");
    var owner = MakeUser(testSuffix, "Owner", "not-used");
    var admin = MakeUser(testSuffix, "Admin", "not-used");
    db.Users.AddRange(customer, other, owner, admin);
    var v = MakeVehicle(owner.Id, testSuffix, "GD3 Core Vehicle A");
    var v2 = MakeVehicle(owner.Id, testSuffix, "GD3 Core Vehicle B");
    var v3 = MakeVehicle(owner.Id, testSuffix, "GD3 Core Vehicle C");
    var v4 = MakeVehicle(owner.Id, testSuffix, "GD3 Core Vehicle D");
    db.Vehicles.AddRange(v, v2, v3, v4);
    var order = MakeOrder(customer.Id, owner.Id, customer.Email, TrangThaiDonThue.DangThue);
    db.RentalOrders.Add(order);
    var a = MakeItem(order.Id, v); var b = MakeItem(order.Id, v2);
    var c = MakeItem(order.Id, v3); var d = MakeItem(order.Id, v4);
    db.RentalOrderItems.AddRange(a, b, c, d);
    var legacyBooking = new Booking
    {
        VehicleId = v.Id, CustomerId = customer.Id, NgayBatDau = DateOnly.FromDateTime(DateTime.Today.AddDays(-9)),
        NgayKetThuc = DateOnly.FromDateTime(DateTime.Today.AddDays(-7)), GiaThueMotNgayLucDat = 120000m,
        TongTien = 240000m, TrangThai = TrangThaiBooking.HoanThanh
    };
    db.Bookings.Add(legacyBooking);
    db.Reviews.Add(new Review
    {
        BookingId = legacyBooking.Id, VehicleId = v.Id, CustomerId = customer.Id,
        SoSao = 3, NhanXet = "GD3 legacy Booking review remains"
    });
    await db.SaveChangesAsync();
    (customerId, otherId, ownerId, adminId, firstItem, secondItem, thirdItem, racingItem, vehicleId, orderId) =
        (customer.Id, other.Id, owner.Id, admin.Id, a.Id, b.Id, c.Id, d.Id, v.Id, order.Id);
}
await using (var db = Open())
{
    var svc = Reviews(db);
    Check(!(await svc.CheckEligibilityAsync(firstItem, customerId)).Allowed, "Cannot review before completed");
    Check((await svc.CheckEligibilityAsync(firstItem, otherId)).Item is null, "Other customer cannot inspect item");
    Check(!(await svc.CreateAsync(new() { RentalOrderItemId = firstItem, SoSao = 5 }, customerId)).Success,
        "Before completed: POST refused");
    Check(!(await svc.CreateAsync(new() { RentalOrderItemId = firstItem, SoSao = 0 }, customerId)).Success,
        "Reject zero stars");
    Check(!(await svc.CreateAsync(new() { RentalOrderItemId = firstItem, SoSao = 6 }, customerId)).Success,
        "Reject six stars");
    Check(!(await svc.CreateAsync(new() { RentalOrderItemId = firstItem, SoSao = 3, NhanXet = new string('a', 1001) }, customerId)).Success,
        "Reject oversized review");
}
await using (var db = Open())
{
    var order = await db.RentalOrders.SingleAsync(o => o.Id == orderId);
    order.TrangThai = TrangThaiDonThue.HoanThanh;
    await db.SaveChangesAsync();
}
await using (var db = Open())
{
    var svc = Reviews(db);
    Check((await svc.CheckEligibilityAsync(firstItem, customerId)).Allowed, "Customer may review completed item");
    Check(!(await svc.CreateAsync(new() { RentalOrderItemId = firstItem, SoSao = 5 }, otherId)).Success,
        "Other customer cannot POST review");
    Check((await svc.CreateAsync(new() { RentalOrderItemId = firstItem, SoSao = 5, NhanXet = "  nice vehicle  " }, customerId)).Success,
        "Valid customer review saved");
    var r = await db.RentalOrderItemReviews.SingleAsync(x => x.RentalOrderItemId == firstItem);
    Check(r.VehicleId == vehicleId && r.CustomerId == customerId && r.NhanXet == "nice vehicle",
        "IDs are server-derived; review comment is trimmed");
    Check(!(await svc.CreateAsync(new() { RentalOrderItemId = firstItem, SoSao = 3 }, customerId)).Success,
        "Second review of same item refused");
    Check((await svc.CreateAsync(new() { RentalOrderItemId = secondItem, SoSao = 2 }, customerId)).Success,
        "Second vehicle item independently reviewable");
    Check((await svc.CreateAsync(new() { RentalOrderItemId = thirdItem, SoSao = 4 }, customerId)).Success,
        "Third vehicle item independently reviewable");
    var map = await svc.GetByOrderAsync(orderId);
    Check(map.Count == 3, "Order review map contains all items");
    Check((await svc.GetOwnerReviewsAsync(ownerId)).Count == 3, "Owner sees only own vehicle's reviews");
    Check((await svc.GetOwnerReviewsAsync(otherId)).Count == 0, "Unrelated owner sees no reviews");
    Check((await svc.GetPublicByVehicleAsync(vehicleId)).Count == 1, "Public list includes new review for vehicle A only");
    var filtered = await svc.GetAdminReviewsAsync(5);
    Check(filtered.OrderReviews.Count == 1 && filtered.OrderReviews[0].SoSao == 5, "Admin stars filter");
    Check((await svc.GetAdminReviewsAsync(3)).LegacyReviews.Any(x => x.VehicleId == vehicleId),
        "Legacy Booking review preserved in Admin view");
    Check(!await svc.SetVisibilityAsync(r.Id, false, ownerId), "Owner cannot moderate");
    Check(await svc.SetVisibilityAsync(r.Id, false, adminId), "Admin can hide");
    Check((await svc.GetPublicByVehicleAsync(vehicleId)).Count == 0, "Hidden review removed from public rating");
    Check(await svc.SetVisibilityAsync(r.Id, true, adminId), "Admin can restore");
    Check((await svc.GetPublicByVehicleAsync(vehicleId)).Count == 1, "Restored review reappears");
}
// Database uniqueness, not merely a service-level duplicate lookup.
await using (var db = Open())
{
    var duplicate = new RentalOrderItemReview
        { RentalOrderItemId = firstItem, VehicleId = vehicleId, CustomerId = customerId, SoSao = 1 };
    db.RentalOrderItemReviews.Add(duplicate);
    var rejected = false;
    try { await db.SaveChangesAsync(); }
    catch (DbUpdateException) { rejected = true; }
    Check(rejected, "MySQL unique index prevents duplicate rows, including concurrent POSTs");
}
// Two independent requests competing to create the same item's first review.
async Task<bool> ConcurrentAttempt()
{
    await using var db = Open();
    var outcome = await Reviews(db).CreateAsync(new OrderReviewCreateViewModel
    {
        RentalOrderItemId = racingItem, SoSao = 5, NhanXet = "race"
    }, customerId);
    return outcome.Success;
}
var raceResults = await Task.WhenAll(ConcurrentAttempt(), ConcurrentAttempt());
Check(raceResults.Count(x => x) == 1, "Exactly one of two concurrent review submissions succeeds");
await using (var db = Open())
    Check(await db.RentalOrderItemReviews.CountAsync(x => x.RentalOrderItemId == racingItem) == 1,
        "Exactly one persisted review after race");
Console.WriteLine("GD3 CORE TESTS PASS. Legacy Booking Review untouched. MySQL disposable only.");
