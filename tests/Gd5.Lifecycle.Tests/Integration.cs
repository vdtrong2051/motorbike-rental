using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using RentalPlatform.Data;
using RentalPlatform.Models.Entities;
using RentalPlatform.Repositories;
using RentalPlatform.Services;
using RentalPlatform.Services.Availability;
using RentalPlatform.Services.Lifecycle;

internal static class Integration
{
    public static async Task RunAsync()
    {
        if (Environment.GetEnvironmentVariable("GD5C_TEST_DATABASE") != "I_UNDERSTAND_DISPOSABLE")
            throw new InvalidOperationException("GD5-C integration requires explicit disposable database consent.");
        var connection = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? throw new InvalidOperationException("Missing isolated MySQL QA connection string.");
        var values = new DbConnectionStringBuilder { ConnectionString = connection };
        static string Value(DbConnectionStringBuilder b, string key) =>
            b.TryGetValue(key, out var value) ? Convert.ToString(value) ?? "" : "";
        var server = Value(values, "Server");
        var dbName = Value(values, "Database");
        if (server is not ("localhost" or "127.0.0.1")
            || !string.Equals(dbName, "motorbike_gd5c_qa", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                "Refusing integration: only localhost / motorbike_gd5c_qa is permitted; NEVER use RentalPlatformDb.");

        var options = new DbContextOptionsBuilder<AppDbContext>().UseMySQL(connection).Options;
        AppDbContext Open() => new(options);
        void Check(bool ok, string label)
        {
            if (!ok) throw new InvalidOperationException("GD5-C MYSQL FAIL: " + label);
            Console.WriteLine("PASS " + label);
        }
        var suffix = Guid.NewGuid().ToString("N")[..12];
        var customer = new User { HoTen="GD5-C QA Customer", Email=$"gd5c-c-{suffix}@example.test",
            PasswordHash="test", Role=UserRole.Customer };
        var owner = new User { HoTen="GD5-C QA Owner", Email=$"gd5c-o-{suffix}@example.test",
            PasswordHash="test", Role=UserRole.Owner };
        Vehicle NewVehicle(string plate) => new()
        {
            OwnerId=owner.Id, TenXe="GD5-C QA Vehicle", LoaiXe="Xe số", BienSo=plate,
            SoChoNgoi=2, HopSo=HopSoXe.SoSan, NhienLieu=NhienLieuXe.Xang,
            Nam=2025, KhuVuc="QA", GiaThueMotNgay=100000m, TrangThai=TrangThaiXe.SanSang
        };
        var v1=NewVehicle("GD5C"+suffix[..8]);
        var v2=NewVehicle("GD5D"+suffix[..8]);
        var pickup=RentalLifecyclePolicy.VietnamNow().AddMinutes(-10);
        var returnTime=pickup.AddDays(1);
        var paymentRef="PAY-GD5C-"+suffix;
        var order=new RentalOrder
        {
            MaDon="GD5C-"+suffix,CustomerId=customer.Id,OwnerId=owner.Id,
            HoTenKhach=customer.HoTen,EmailKhach=customer.Email,SDTKhach="0901234567",
            SoCCCD="123456789012",SoGPLX="123456789",DiaChiKhach="QA",
            TienThue=100000m,TienDatCoc=20000m,PhiDichVu=5000m,TongTien=125000m,
            TrangThai=TrangThaiDonThue.DaThanhToan,
            TrangThaiThanhToan=TrangThaiThanhToanDon.DaThanhToan,
            MaThamChieuThanhToan=paymentRef,
            Contract=new RentalOrderContract { SoHopDong="HD-GD5C-"+suffix,NoiDungHopDong="GD5-C QA",
                HoTenChuXe=owner.HoTen,EmailChuXe=owner.Email,DaKyKhach=true,DaKyChuXe=true }
        };
        order.Items.Add(new RentalOrderItem { VehicleId=v1.Id,TenXe=v1.TenXe, BienSo=v1.BienSo,
            LoaiXe=v1.LoaiXe,TinhTrangLucDat="QA",NgayNhanXe=pickup,NgayTraXe=returnTime,
            DonGiaNgay=100000m,SoNgayThue=1,ThanhTien=100000m });
        order.Payments.Add(new RentalOrderPayment { MaGiaoDich=paymentRef,
            PhuongThuc=PhuongThucThanhToanDon.ChuyenKhoan,TrangThai=TrangThaiThanhToanDon.DaThanhToan,
            SoTien=125000m,PaidAt=DateTime.UtcNow });

        var booking=new Booking
        {
            VehicleId=v2.Id, CustomerId=customer.Id,
            NgayBatDau=DateOnly.FromDateTime(RentalLifecyclePolicy.VietnamNow()),
            NgayKetThuc=DateOnly.FromDateTime(RentalLifecyclePolicy.VietnamNow()).AddDays(1),
            GiaThueMotNgayLucDat=100000m, TongTien=100000m,
            TrangThai=TrangThaiBooking.DaXacNhan
        };
        booking.Contract=new Contract { BookingId=booking.Id, SoHopDong="HD-GD5D-"+suffix,
            NoiDungHopDong="GD5-C QA",DaKyKhach=true,DaKyChuXe=true };

        await using (var db=Open())
        {
            db.Users.AddRange(owner,customer);
            db.Vehicles.AddRange(v1,v2);
            db.RentalOrders.Add(order);
            db.Bookings.Add(booking);
            db.Payments.Add(new Payment { BookingId=booking.Id,LoaiThanhToan=LoaiThanhToan.ThanhToanConLai,
                PhuongThuc=PhuongThucThanhToan.TienMat,SoTien=100000m,
                MaGiaoDich="GD5C-PAY-"+suffix,TrangThai=TrangThaiThanhToan.ThanhCong });
            await db.SaveChangesAsync();
        }

        await using (var db=Open())
        {
            var orders=new RentalOrderService(db,null!,null!,new VehicleAvailabilityService(db));
            Check((await orders.HandOverAsync(order.Id,owner.Id)).Success,"Multi-order handover succeeds once");
            Check(!(await orders.HandOverAsync(order.Id,owner.Id)).Success,"Multi-order duplicate handover rejected");
            Check((await db.Vehicles.AsNoTracking().SingleAsync(v=>v.Id==v1.Id)).TrangThai==TrangThaiXe.DangThue,
                "Vehicle physically occupied after handover");
            Check((await orders.CompleteAsync(order.Id,owner.Id)).Success,"Multi-order return commits");
            Check(!(await orders.CompleteAsync(order.Id,owner.Id)).Success,"Duplicate return rejected");
            Check((await db.Vehicles.AsNoTracking().SingleAsync(v=>v.Id==v1.Id)).TrangThai==TrangThaiXe.SanSang,
                "Returned vehicle ready again");
            Check(!(await orders.SignContractAsync(order.Id,owner.Id)).Success,"Completed order cannot be re-signed");
        }
        await using (var db=Open())
        {
            var legacy=new BookingService(new BookingRepository(db),new VehicleRepository(db),null!,
                new PaymentRepository(db),db,new VehicleAvailabilityService(db));
            Check(!(await legacy.HuyAsync(booking.Id,customer.Id,"refund pending")).Success,
                "Paid legacy booking cannot be silently cancelled");
            Check((await legacy.XacNhanGiaoXeAsync(booking.Id,owner.Id)).Success,"Legacy paid booking handover succeeds");
            Check(!(await legacy.XacNhanGiaoXeAsync(booking.Id,owner.Id)).Success,"Legacy duplicate handover rejected");
            Check((await legacy.XacNhanTraXeAsync(booking.Id,owner.Id)).Success,"Legacy return succeeds");
            Check(!(await legacy.XacNhanTraXeAsync(booking.Id,owner.Id)).Success,"Legacy duplicate return rejected");
        }
        Console.WriteLine("GD5-C MySQL integration PASS. Fixtures remain in disposable QA database.");
    }
}
