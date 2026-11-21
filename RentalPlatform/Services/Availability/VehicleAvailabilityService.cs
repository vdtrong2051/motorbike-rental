using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using RentalPlatform.Data;
using RentalPlatform.Models.Entities;

namespace RentalPlatform.Services.Availability;

// All intervals are [Start, End): a vehicle can be picked up at the exact return time.
public readonly record struct AvailabilityWindow(DateTime Start, DateTime End)
{
    public bool IsValid => End > Start;
    public bool Overlaps(AvailabilityWindow other) => Start < other.End && other.Start < End;

    // Legacy Booking stores inclusive calendar dates; its last date remains reserved.
    public static AvailabilityWindow FromLegacy(DateOnly from, DateOnly to) =>
        new(from.ToDateTime(TimeOnly.MinValue), to.AddDays(1).ToDateTime(TimeOnly.MinValue));
}

public interface IVehicleAvailabilityService
{
    Task<bool> IsBusyAsync(Guid vehicleId, DateTime start, DateTime end,
        Guid? excludedRentalOrderId = null, Guid? excludedBookingId = null);
    Task<bool> IsBookableAsync(Guid vehicleId, DateTime start, DateTime end);
    Task<IReadOnlyList<AvailabilityWindow>> GetBusyWindowsAsync(Guid vehicleId, DateTime start, DateTime end);
    Task LockVehiclesAsync(IEnumerable<Guid> vehicleIds);
}

public sealed class VehicleAvailabilityService : IVehicleAvailabilityService
{
    private readonly AppDbContext _db;
    public VehicleAvailabilityService(AppDbContext db) => _db = db;

    public static bool IsOperatingState(TrangThaiXe state) =>
        state is TrangThaiXe.SanSang or TrangThaiXe.DangThue;

    public async Task<bool> IsBookableAsync(Guid vehicleId, DateTime start, DateTime end)
    {
        if (end <= start) return false;
        var status = await _db.Vehicles.AsNoTracking()
            .Where(v => v.Id == vehicleId).Select(v => (TrangThaiXe?)v.TrangThai).FirstOrDefaultAsync();
        return status.HasValue && IsOperatingState(status.Value)
            && !await IsBusyAsync(vehicleId, start, end);
    }

    public async Task<bool> IsBusyAsync(Guid vehicleId, DateTime start, DateTime end,
        Guid? excludedRentalOrderId = null, Guid? excludedBookingId = null)
    {
        if (end <= start) throw new ArgumentException("Thời gian trả xe phải sau nhận xe.");
        // DateOnly legacy bookings reserve all of the last calendar day.
        var firstDate = DateOnly.FromDateTime(start);
        var lastTouchedDate = DateOnly.FromDateTime(end.AddTicks(-1));
        var legacy = await _db.Bookings.AsNoTracking().AnyAsync(b => b.VehicleId == vehicleId
            && (excludedBookingId == null || b.Id != excludedBookingId)
            && b.TrangThai != TrangThaiBooking.DaHuy
            && b.NgayBatDau <= lastTouchedDate && b.NgayKetThuc >= firstDate);
        if (legacy) return true;
        return await _db.RentalOrderItems.AsNoTracking().AnyAsync(i => i.VehicleId == vehicleId
            && (excludedRentalOrderId == null || i.RentalOrderId != excludedRentalOrderId)
            && i.RentalOrder.TrangThai != TrangThaiDonThue.DaHuy
            && i.NgayNhanXe < end && i.NgayTraXe > start);
    }

    public async Task<IReadOnlyList<AvailabilityWindow>> GetBusyWindowsAsync(Guid vehicleId, DateTime start, DateTime end)
    {
        if (end <= start || (end - start).TotalDays > 370)
            throw new ArgumentException("Khoảng xem lịch không hợp lệ hoặc quá dài.");
        var first = DateOnly.FromDateTime(start);
        var last = DateOnly.FromDateTime(end.AddTicks(-1));
        var bookings = await _db.Bookings.AsNoTracking()
            .Where(b => b.VehicleId == vehicleId && b.TrangThai != TrangThaiBooking.DaHuy
                && b.NgayBatDau <= last && b.NgayKetThuc >= first)
            .Select(b => new { b.NgayBatDau, b.NgayKetThuc }).ToListAsync();
        var orders = await _db.RentalOrderItems.AsNoTracking()
            .Where(i => i.VehicleId == vehicleId && i.RentalOrder.TrangThai != TrangThaiDonThue.DaHuy
                && i.NgayNhanXe < end && i.NgayTraXe > start)
            .Select(i => new { i.NgayNhanXe, i.NgayTraXe }).ToListAsync();
        return bookings.Select(b => AvailabilityWindow.FromLegacy(b.NgayBatDau, b.NgayKetThuc))
            .Concat(orders.Select(i => new AvailabilityWindow(i.NgayNhanXe, i.NgayTraXe)))
            .Where(w => w.Overlaps(new AvailabilityWindow(start, end)))
            .OrderBy(w => w.Start).ToList();
    }

    // Must be called inside an active EF transaction. Both old and new order paths
    // acquire the SAME Vehicle row lock, in stable Id order, before checking slots.
    public async Task LockVehiclesAsync(IEnumerable<Guid> vehicleIds)
    {
        var transaction = _db.Database.CurrentTransaction
            ?? throw new InvalidOperationException("Vehicle lock requires an active transaction.");
        var connection = _db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
            throw new InvalidOperationException("Transaction connection is not open.");
        foreach (var vehicleId in vehicleIds.Distinct().OrderBy(id => id))
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction.GetDbTransaction();
            command.CommandText = "SELECT `Id` FROM `Vehicles` WHERE `Id` = @vehicleId FOR UPDATE";
            command.CommandTimeout = 15;
            var parameter = command.CreateParameter();
            parameter.ParameterName = "@vehicleId";
            parameter.Value = vehicleId.ToString(); // MySQL EF schema stores GUID as char(36).
            command.Parameters.Add(parameter);
            if (await command.ExecuteScalarAsync() is null)
                throw new InvalidOperationException("Xe không tồn tại hoặc vừa bị xóa.");
        }
    }

    // Apply the very same reservation/status policy in server-side catalogue filtering.
    public static IQueryable<Vehicle> FilterAvailable(IQueryable<Vehicle> source,
        AppDbContext db, DateTime start, DateTime end)
    {
        var first = DateOnly.FromDateTime(start);
        var last = DateOnly.FromDateTime(end.AddTicks(-1));
        return source.Where(v =>
            (v.TrangThai == TrangThaiXe.SanSang || v.TrangThai == TrangThaiXe.DangThue)
            && !db.Bookings.Any(b => b.VehicleId == v.Id
                && b.TrangThai != TrangThaiBooking.DaHuy
                && b.NgayBatDau <= last && b.NgayKetThuc >= first)
            && !db.RentalOrderItems.Any(i => i.VehicleId == v.Id
                && i.RentalOrder.TrangThai != TrangThaiDonThue.DaHuy
                && i.NgayNhanXe < end && i.NgayTraXe > start));
    }
}
