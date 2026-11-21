using RentalPlatform.Models.Entities;
using RentalPlatform.Services.Lifecycle;

static void Check(bool condition, string name)
{
    if (!condition) throw new InvalidOperationException("GD5-C FAIL: " + name);
    Console.WriteLine("PASS " + name);
}

var local = new DateTime(2027, 1, 15, 8, 0, 0);
var paid = new RentalOrder
{
    TrangThai = TrangThaiDonThue.DaThanhToan,
    TrangThaiThanhToan = TrangThaiThanhToanDon.DaThanhToan,
    TienThue = 100000m, TienDatCoc = 20000m, PhiDichVu = 5000m, TongTien = 125000m,
    Contract = new RentalOrderContract { DaKyKhach = true, DaKyChuXe = true },
    Items = new List<RentalOrderItem> { new() { NgayNhanXe = local.AddMinutes(-20), NgayTraXe = local.AddDays(1) } }
};
Check(RentalLifecyclePolicy.ValidateHandover(paid,local) is null, "Valid paid signed pickup");
paid.Contract!.DaKyChuXe=false;
Check(RentalLifecyclePolicy.ValidateHandover(paid,local) is not null, "Unsigned contract blocks handover");
paid.Contract.DaKyChuXe=true;
paid.TrangThaiThanhToan=TrangThaiThanhToanDon.ChoThanhToan;
Check(RentalLifecyclePolicy.ValidateHandover(paid,local) is not null, "Unpaid order blocks handover");
paid.TrangThaiThanhToan=TrangThaiThanhToanDon.DaThanhToan;
paid.Items.First().NgayNhanXe=local.AddHours(3);
Check(RentalLifecyclePolicy.ValidateHandover(paid,local) is not null, "Premature pickup blocks handover");
paid.Items.First().NgayNhanXe=local.AddHours(2);
Check(RentalLifecyclePolicy.ValidateHandover(paid,local) is null, "Two-hour early pickup allowed");
paid.Items.First().NgayTraXe=local;
Check(RentalLifecyclePolicy.ValidateHandover(paid,local) is not null, "Expired rental blocks pickup");
paid.Items.First().NgayTraXe=local.AddDays(1);
paid.TrangThai=TrangThaiDonThue.HoanThanh;
Check(RentalLifecyclePolicy.ValidateHandover(paid,local) is not null, "Completed order cannot handover again");
Check(!RentalLifecyclePolicy.CanCancel(TrangThaiDonThue.DangThue), "Renting cannot be cancelled");
Check(!RentalLifecyclePolicy.CanCancel(TrangThaiDonThue.HoanThanh), "Completed cannot be cancelled");
Check(!RentalLifecyclePolicy.CanCancel(TrangThaiDonThue.DaHuy), "Cancelled cannot be cancelled again");
Check(RentalLifecyclePolicy.CanCancel(TrangThaiDonThue.DaThanhToan), "Paid uncollected order can request cancellation");
Check(!RentalLifecyclePolicy.CanSignContract(TrangThaiDonThue.ChoChuXeXacNhan), "Pending confirmation cannot sign");
Check(RentalLifecyclePolicy.CanSignContract(TrangThaiDonThue.DaXacNhan), "Confirmed can sign");
Check(!RentalLifecyclePolicy.CanSignContract(TrangThaiDonThue.HoanThanh), "Completed cannot sign");
Check(!RentalLifecyclePolicy.CanSignContract(TrangThaiDonThue.DaHuy), "Cancelled cannot sign");
Check(RentalLifecyclePolicy.CanRestoreVehicleAvailability(TrangThaiXe.DangThue,false), "Returned idle vehicle becomes ready");
Check(!RentalLifecyclePolicy.CanRestoreVehicleAvailability(TrangThaiXe.BaoTri,false), "Maintenance state preserved");
Check(!RentalLifecyclePolicy.CanRestoreVehicleAvailability(TrangThaiXe.DangThue,true), "Other physical rental remains protected");
paid.TrangThai=TrangThaiDonThue.DaThanhToan;
Check(RentalLifecyclePolicy.FinancialBreakdownMatches(paid), "Price breakdown matches order total");
paid.TongTien++;
Check(!RentalLifecyclePolicy.FinancialBreakdownMatches(paid), "Mismatched total detected");
Check(RentalLifecyclePolicy.VietnamFromUtc(new DateTime(2027,1,15,1,0,0,DateTimeKind.Utc)) == local,
    "UTC converted to Vietnam civil time without machine local timezone");
var old = new Booking { TrangThai=TrangThaiBooking.DaXacNhan,
    NgayBatDau=new DateOnly(2027,1,15),NgayKetThuc=new DateOnly(2027,1,16),
    Contract=new Contract { DaKyKhach=true,DaKyChuXe=true } };
Check(RentalLifecyclePolicy.ValidateLegacyHandover(old,local) is null, "Legacy booking can handover on pickup day");
Check(RentalLifecyclePolicy.ValidateLegacyHandover(old,local.AddDays(-1)) is not null, "Legacy early handover refused");
Check(RentalLifecyclePolicy.ValidateLegacyHandover(old,local.AddDays(2)) is not null, "Legacy expired handover refused");
Console.WriteLine("GD5-C PURE LIFECYCLE TESTS PASS (no database writes). MySQL/browser acceptance is separate.");
if (args.Length == 1 && args[0] == "integration") await Integration.RunAsync();
