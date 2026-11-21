using System.Data;
using Microsoft.EntityFrameworkCore;
using RentalPlatform.Data;
using RentalPlatform.Models.Entities;
using RentalPlatform.Services.Availability;

internal static class Integration
{
    public static async Task RunAsync()
    {
        if (Environment.GetEnvironmentVariable("GD5B_TEST_DATABASE") != "I_UNDERSTAND_DISPOSABLE")
            throw new InvalidOperationException("Integration requires GD5B_TEST_DATABASE=I_UNDERSTAND_DISPOSABLE.");
        var connection = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? throw new InvalidOperationException("Missing isolated MySQL connection string.");
        if (!(connection.Contains("Server=localhost",StringComparison.OrdinalIgnoreCase) ||
              connection.Contains("Server=127.0.0.1",StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("GD5-B test only permits localhost MySQL.");
        var dbName = connection.Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim()).FirstOrDefault(x => x.StartsWith("Database=", StringComparison.OrdinalIgnoreCase))
            ?.Split('=',2)[1] ?? "";
        if (!dbName.Contains("gd5b", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Database must contain gd5b in its name (disposable test DB only).");

        var options = new DbContextOptionsBuilder<AppDbContext>().UseMySQL(connection).Options;
        AppDbContext Open() => new(options);
        void Ensure(bool condition, string title)
        {
            if (!condition) throw new InvalidOperationException("GD5-B INTEGRATION FAIL: " + title);
            Console.WriteLine("PASS " + title);
        }
        var suffix = Guid.NewGuid().ToString("N")[..12];
        User customer = new() { Email=$"gd5bc-{suffix}@example.test", HoTen="GD5-B Customer", Role=UserRole.Customer, PasswordHash="test" };
        User owner = new() { Email=$"gd5bo-{suffix}@example.test", HoTen="GD5-B Owner", Role=UserRole.Owner, PasswordHash="test" };
        Vehicle vehicle = new()
        {
            OwnerId = owner.Id, TenXe = "GD5-B QA vehicle", LoaiXe = "Xe số", BienSo = "GD5B"+suffix[..8],
            SoChoNgoi=2, HopSo=HopSoXe.SoSan, NhienLieu=NhienLieuXe.Xang,
            Nam=2025, KhuVuc="QA", GiaThueMotNgay=100000, TrangThai=TrangThaiXe.SanSang
        };
        await using (var db=Open())
        {
            db.Users.AddRange(owner,customer); db.Vehicles.Add(vehicle); await db.SaveChangesAsync();
        }
        var first=new DateTime(2027,1,15,8,0,0);
        var last=first.AddDays(2);
        await using (var db=Open())
        {
            var availability=new VehicleAvailabilityService(db);
            Ensure(await availability.IsBookableAsync(vehicle.Id,first,last),"Initially free");
            var legacy=new Booking { VehicleId=vehicle.Id,CustomerId=customer.Id,
                NgayBatDau=DateOnly.FromDateTime(first), NgayKetThuc=DateOnly.FromDateTime(last),
                TrangThai=TrangThaiBooking.ChoXacNhan, GiaThueMotNgayLucDat=100000,TongTien=200000 };
            db.Bookings.Add(legacy); await db.SaveChangesAsync();
            Ensure(await availability.IsBusyAsync(vehicle.Id,first,last),"Pending legacy Booking blocks new order");
            legacy.TrangThai=TrangThaiBooking.DaHuy; await db.SaveChangesAsync();
            Ensure(!await availability.IsBusyAsync(vehicle.Id,first,last),"Cancelled legacy Booking releases slot");
            var order=new RentalOrder { MaDon="GD5B-"+suffix, CustomerId=customer.Id,OwnerId=owner.Id,
                HoTenKhach="QA",EmailKhach=customer.Email,SDTKhach="0901234567",
                SoCCCD="123456789012",SoGPLX="123456789",DiaChiKhach="QA",
                TongTien=200000,TienThue=200000,TrangThai=TrangThaiDonThue.ChoChuXeXacNhan };
            order.Items.Add(new RentalOrderItem { VehicleId=vehicle.Id,TenXe=vehicle.TenXe,
                BienSo=vehicle.BienSo,LoaiXe=vehicle.LoaiXe,TinhTrangLucDat="QA",
                NgayNhanXe=first,NgayTraXe=last,DonGiaNgay=100000,SoNgayThue=2,ThanhTien=200000 });
            db.RentalOrders.Add(order); await db.SaveChangesAsync();
            Ensure(await availability.IsBusyAsync(vehicle.Id,first,last),"Pending RentalOrder blocks old Booking");
            Ensure(!await availability.IsBusyAsync(vehicle.Id,first,last,excludedRentalOrderId:order.Id),"Self order excluded from confirmation recheck");
            Ensure((await availability.GetBusyWindowsAsync(vehicle.Id,first,last)).Count==1,"Public slot list has no personal data");
            order.TrangThai=TrangThaiDonThue.DaHuy; await db.SaveChangesAsync();
            Ensure(!await availability.IsBusyAsync(vehicle.Id,first,last),"Cancelled RentalOrder releases slot");
        }
        // Two independent requests on the exact same vehicle/range, both using
        // the same SQL row lock (not an in-memory process lock).
        async Task<bool> Compete()
        {
            try
            {
                await using var db=Open();
                var availability=new VehicleAvailabilityService(db);
                await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
                await availability.LockVehiclesAsync(new[]{vehicle.Id});
                if (await availability.IsBusyAsync(vehicle.Id,first,last)) return false;
                db.Bookings.Add(new Booking { VehicleId=vehicle.Id,CustomerId=customer.Id,
                    NgayBatDau=DateOnly.FromDateTime(first), NgayKetThuc=DateOnly.FromDateTime(last),
                    TrangThai=TrangThaiBooking.ChoXacNhan,GiaThueMotNgayLucDat=100000,TongTien=200000 });
                await db.SaveChangesAsync(); await tx.CommitAsync(); return true;
            }
            catch (System.Data.Common.DbException) { return false; }
        }
        var results=await Task.WhenAll(Compete(),Compete());
        Ensure(results.Count(v=>v)==1,"Concurrent booking permits only one winner");
        Console.WriteLine("GD5-B MySQL integration PASS. Test fixtures remain in isolated disposable database.");
    }
}
