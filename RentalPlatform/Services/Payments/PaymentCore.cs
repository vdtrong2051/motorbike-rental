using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RentalPlatform.Data;
using RentalPlatform.Models.Entities;
using RentalPlatform.Services;

namespace RentalPlatform.Services.Payments;

public enum PaymentSignalSource { Webhook, Demo, OwnerCash }

public record DemoPaymentSession(Guid OrderId, string Reference, decimal Amount,
    PhuongThucThanhToanDon Method, DateTime ExpiresAtUtc);

public interface IPaymentCore
{
    Task<RentalOrderOperationResult> SelectAsync(Guid orderId, PhuongThucThanhToanDon method, Guid customerId);
    Task<RentalOrderOperationResult> ConfirmAsync(Guid orderId, string reference, decimal? amount,
        PaymentSignalSource source, string? note = null, Guid? ownerId = null);
    Task<RentalOrderOperationResult> FailAsync(Guid orderId, string reference, decimal? amount,
        PaymentSignalSource source, string? reason);
    Task<DemoPaymentSession?> GetDemoSessionAsync(Guid orderId, Guid customerId);
}

/// <summary>
/// GD1: uses the existing RentalOrders/RentalOrderPayments schema. No EF model or migration changes.
/// All state transitions of a given order acquire the SAME MySQL row lock before writes.
/// </summary>
public sealed class PaymentCore : IPaymentCore
{
    private readonly AppDbContext _db;
    private readonly int _pendingMinutes;

    public PaymentCore(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _pendingMinutes = Math.Clamp(config.GetValue("Payment:PendingMinutes", 30), 1, 1440);
    }

    public int PendingMinutes => _pendingMinutes;

    public static bool IsPending(TrangThaiThanhToanDon status) =>
        status is TrangThaiThanhToanDon.ChoThanhToan or TrangThaiThanhToanDon.ChoThuTienKhiNhanXe;

    public static bool IsDemoMethod(PhuongThucThanhToanDon method) =>
        method is PhuongThucThanhToanDon.ChuyenKhoan or PhuongThucThanhToanDon.ViDienTu
            or PhuongThucThanhToanDon.TheThanhToan;

    /// <summary>Call ONLY after BeginTransaction; MySQL/InnoDB pessimistic write lock.</summary>
    public static async Task<RentalOrder?> LockOrderAsync(AppDbContext db, Guid orderId)
    {
        // ToListAsync deliberately avoids a LINQ LIMIT appended AFTER FOR UPDATE.
        var rows = await db.RentalOrders
            .FromSqlInterpolated($"SELECT * FROM `RentalOrders` WHERE `Id` = {orderId} FOR UPDATE")
            .ToListAsync();
        if (rows.Count == 0) return null;
        // Populate relationships into the already-tracked, locked entity.
        return await db.RentalOrders
            .Include(o => o.Contract)
            .Include(o => o.Payments)
            .Include(o => o.Items)
            .FirstAsync(o => o.Id == orderId);
    }

    private bool Expired(RentalOrderPayment payment) =>
        payment.PhuongThuc != PhuongThucThanhToanDon.ThanhToanKhiNhanXe
        && DateTime.UtcNow >= payment.CreatedAt.AddMinutes(_pendingMinutes);

    private static RentalOrderPayment? CurrentPayment(RentalOrder order) =>
        order.Payments.FirstOrDefault(p => string.Equals(p.MaGiaoDich,
            order.MaThamChieuThanhToan, StringComparison.Ordinal));

    private static string Note(string value) => value.Length <= 200 ? value : value[..200];

    public async Task<RentalOrderOperationResult> SelectAsync(Guid orderId, PhuongThucThanhToanDon method, Guid customerId)
    {
        if (!Enum.IsDefined(method)) return new(false, "Phương thức thanh toán không hợp lệ.");
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        var order = await LockOrderAsync(_db, orderId);
        if (order is null || order.CustomerId != customerId)
            return new(false, "Không tìm thấy đơn hoặc bạn không có quyền.");
        if (order.TrangThai is not (TrangThaiDonThue.DaXacNhan or TrangThaiDonThue.ChoThanhToan)
            || order.TrangThaiThanhToan is TrangThaiThanhToanDon.DaThanhToan
                or TrangThaiThanhToanDon.ChoHoanTien or TrangThaiThanhToanDon.DaHoanTien)
            return new(false, "Đơn không ở trạng thái cho phép tạo thanh toán.");
        if (order.Contract is null || !order.Contract.DaKyKhach || !order.Contract.DaKyChuXe)
            return new(false, "Cả khách thuê và chủ xe phải ký hợp đồng trước khi thanh toán.");

        var previous = CurrentPayment(order);
        if (previous is not null && IsPending(previous.TrangThai))
        {
            if (previous.PhuongThuc == method && !Expired(previous))
            {
                await tx.CommitAsync();
                return new(true, Order: order); // retry selection must NOT create a second attempt
            }
            previous.TrangThai = TrangThaiThanhToanDon.ThatBai;
            previous.GhiChuCongThanhToan = Expired(previous) ? "GD1:EXPIRED" : "GD1:REPLACED";
        }

        var status = method == PhuongThucThanhToanDon.ThanhToanKhiNhanXe
            ? TrangThaiThanhToanDon.ChoThuTienKhiNhanXe : TrangThaiThanhToanDon.ChoThanhToan;
        var payment = new RentalOrderPayment
        {
            RentalOrderId = order.Id,
            // 36 chars, cryptographically unpredictable; existing unique index protects uniqueness.
            MaGiaoDich = $"PAY-{Guid.NewGuid():N}",
            PhuongThuc = method,
            TrangThai = status,
            SoTien = order.TongTien
        };
        order.PhuongThucThanhToan = method;
        order.TrangThai = TrangThaiDonThue.ChoThanhToan;
        order.TrangThaiThanhToan = status;
        order.MaThamChieuThanhToan = payment.MaGiaoDich;
        order.UpdatedAt = DateTime.UtcNow;
        _db.RentalOrderPayments.Add(payment);
        await _db.SaveChangesAsync();
        await tx.CommitAsync();
        return new(true, Order: order);
    }

    public async Task<RentalOrderOperationResult> ConfirmAsync(Guid orderId, string reference, decimal? amount,
        PaymentSignalSource source, string? note = null, Guid? ownerId = null)
    {
        if (string.IsNullOrWhiteSpace(reference) || !amount.HasValue || amount.Value <= 0)
            return new(false, "Thiếu mã giao dịch hoặc số tiền hợp lệ.");
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        var order = await LockOrderAsync(_db, orderId);
        if (order is null) return new(false, "Không tìm thấy đơn.");
        if (!string.Equals(order.MaThamChieuThanhToan, reference, StringComparison.Ordinal))
            return new(false, "Mã tham chiếu thanh toán không khớp hoặc đã bị thay thế.");
        if (order.TongTien != amount.Value)
            return new(false, "Số tiền thanh toán không khớp tổng đơn.");
        if (order.TrangThai == TrangThaiDonThue.DaHuy)
            return new(false, "Đơn đã hủy; callback đến muộn không được ghi nhận.");
        var payment = CurrentPayment(order);
        if (payment is null || payment.SoTien != amount.Value)
            return new(false, "Không tìm thấy giao dịch với số tiền hợp lệ.");
        if (source == PaymentSignalSource.OwnerCash)
        {
            if (!ownerId.HasValue || order.OwnerId != ownerId.Value
                || payment.PhuongThuc != PhuongThucThanhToanDon.ThanhToanKhiNhanXe)
                return new(false, "Chỉ đúng chủ xe mới được xác nhận thu tiền mặt.");
        }
        else if (!IsDemoMethod(payment.PhuongThuc))
            return new(false, "Webhook/demo không được xác nhận giao dịch tiền mặt.");

        // Idempotent success is safe even after hand-over; no second revenue record is created.
        if (payment.TrangThai == TrangThaiThanhToanDon.DaThanhToan
            && order.TrangThaiThanhToan == TrangThaiThanhToanDon.DaThanhToan)
        {
            await tx.CommitAsync();
            return new(true, Order: order);
        }
        if (order.TrangThai != TrangThaiDonThue.ChoThanhToan
            || !IsPending(order.TrangThaiThanhToan) || !IsPending(payment.TrangThai))
            return new(false, "Giao dịch không còn ở trạng thái có thể xác nhận.");
        if (source != PaymentSignalSource.OwnerCash && Expired(payment))
        {
            payment.TrangThai = TrangThaiThanhToanDon.ThatBai;
            payment.GhiChuCongThanhToan = "GD1:EXPIRED";
            order.TrangThaiThanhToan = TrangThaiThanhToanDon.ThatBai;
            order.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            return new(false, "Giao dịch đã hết hạn. Vui lòng tạo lần thanh toán mới.");
        }
        var now = DateTime.UtcNow;
        payment.TrangThai = TrangThaiThanhToanDon.DaThanhToan;
        payment.PaidAt = now;
        payment.GhiChuCongThanhToan = Note($"GD1:{source}:{note ?? "Confirmed"}");
        order.TrangThaiThanhToan = TrangThaiThanhToanDon.DaThanhToan;
        order.TrangThai = TrangThaiDonThue.DaThanhToan;
        order.PaidAt = now;
        order.UpdatedAt = now;
        await _db.SaveChangesAsync();
        await tx.CommitAsync();
        return new(true, Order: order);
    }

    public async Task<RentalOrderOperationResult> FailAsync(Guid orderId, string reference, decimal? amount,
        PaymentSignalSource source, string? reason)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        var order = await LockOrderAsync(_db, orderId);
        if (order is null || order.TrangThai == TrangThaiDonThue.DaHuy
            || !string.Equals(order.MaThamChieuThanhToan, reference, StringComparison.Ordinal))
            return new(false, "Không tìm thấy giao dịch demo còn hiệu lực.");
        var payment = CurrentPayment(order);
        if (payment is null || !IsDemoMethod(payment.PhuongThuc)
            || (source is not (PaymentSignalSource.Demo or PaymentSignalSource.Webhook))
            || (source == PaymentSignalSource.Webhook
                && (!amount.HasValue || amount.Value != order.TongTien || payment.SoTien != amount.Value)))
            return new(false, "Phương thức hoặc số tiền giao dịch không hợp lệ.");
        if (payment.TrangThai == TrangThaiThanhToanDon.ThatBai
            && order.TrangThaiThanhToan == TrangThaiThanhToanDon.ThatBai)
        {
            await tx.CommitAsync();
            return new(true, Order: order);
        }
        if (order.TrangThai != TrangThaiDonThue.ChoThanhToan
            || payment.TrangThai != TrangThaiThanhToanDon.ChoThanhToan
            || order.TrangThaiThanhToan != TrangThaiThanhToanDon.ChoThanhToan)
            return new(false, "Giao dịch đã kết thúc; không thể ghi đè kết quả.");
        payment.TrangThai = TrangThaiThanhToanDon.ThatBai;
        payment.GhiChuCongThanhToan = Expired(payment) ? "GD1:EXPIRED" : Note($"GD1:{source}:FAILED:{reason ?? "Declined"}");
        order.TrangThaiThanhToan = TrangThaiThanhToanDon.ThatBai;
        order.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await tx.CommitAsync();
        return new(true, Order: order);
    }

    public async Task<DemoPaymentSession?> GetDemoSessionAsync(Guid orderId, Guid customerId)
    {
        var order = await _db.RentalOrders.AsNoTracking().Include(o => o.Payments)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.CustomerId == customerId);
        if (order is null || order.TrangThai != TrangThaiDonThue.ChoThanhToan
            || order.TrangThaiThanhToan != TrangThaiThanhToanDon.ChoThanhToan)
            return null;
        var payment = CurrentPayment(order);
        if (payment is null || !IsDemoMethod(payment.PhuongThuc)
            || payment.TrangThai != TrangThaiThanhToanDon.ChoThanhToan || Expired(payment))
            return null;
        return new(order.Id, payment.MaGiaoDich, payment.SoTien, payment.PhuongThuc,
            payment.CreatedAt.AddMinutes(_pendingMinutes));
    }
}
