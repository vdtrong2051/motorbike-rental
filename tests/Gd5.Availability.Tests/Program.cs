using RentalPlatform.Models.Entities;
using RentalPlatform.Services.Availability;

static void Assert(bool ok, string message)
{
    if (!ok) throw new InvalidOperationException("GD5-B FAIL: " + message);
    Console.WriteLine("PASS " + message);
}
var start = new DateTime(2026, 11, 23, 8, 0, 0);
var end = new DateTime(2026, 11, 25, 8, 0, 0);
var first = new AvailabilityWindow(start, end);
Assert(first.IsValid, "Valid rental interval");
Assert(!new AvailabilityWindow(end, start).IsValid, "Reject reversed interval");
Assert(!first.Overlaps(new AvailabilityWindow(end, end.AddDays(1))), "Touching half-open boundaries are free");
Assert(first.Overlaps(new AvailabilityWindow(start.AddDays(1),end.AddDays(1))), "Partial overlap blocked");
Assert(first.Overlaps(new AvailabilityWindow(start,end)), "Exact overlap blocked");
Assert(first.Overlaps(new AvailabilityWindow(start.AddHours(1),end.AddHours(-1))), "Contained interval blocked");
Assert(!first.Overlaps(new AvailabilityWindow(start.AddDays(-2),start)), "Previous period ending at pickup is free");
var legacy = AvailabilityWindow.FromLegacy(new DateOnly(2026,11,23),new DateOnly(2026,11,25));
Assert(legacy.End == new DateTime(2026,11,26), "Legacy last day stays reserved");
Assert(legacy.Overlaps(new AvailabilityWindow(new DateTime(2026,11,25,18,0,0),new DateTime(2026,11,26))), "Legacy last day occupied");
Assert(!legacy.Overlaps(new AvailabilityWindow(new DateTime(2026,11,26),new DateTime(2026,11,27))), "Legacy release boundary");
Assert(VehicleAvailabilityService.IsOperatingState(TrangThaiXe.SanSang), "Available operational state");
Assert(VehicleAvailabilityService.IsOperatingState(TrangThaiXe.DangThue), "Currently rented may be prebooked");
Assert(!VehicleAvailabilityService.IsOperatingState(TrangThaiXe.BaoTri), "Maintenance rejected");
Assert(!VehicleAvailabilityService.IsOperatingState(TrangThaiXe.ChoDuyet), "Unapproved vehicle rejected");
Console.WriteLine("GD5-B PURE CORE TESTS PASS (no database writes). Integration QA must be run separately on disposable MySQL.");

if (args.Length == 1 && args[0] == "integration") await Integration.RunAsync();
