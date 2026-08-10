using System.Data;
using System.Text;
using Microsoft.EntityFrameworkCore;
using RentalPlatform.Data;
using RentalPlatform.Models.Entities;
using RentalPlatform.Models.ViewModels.RentalOrder;

namespace RentalPlatform.Services;

public record RentalOrderOperationResult(bool Success, string? Error = null, RentalOrder? Order = null);

public interface IRentalOrderService
{
    Task<CheckoutViewModel> BuildCheckoutAsync(Guid customerId);
    Task<RentalOrderOperationResult> CreateAsync(CheckoutViewModel model, Guid customerId);
    Task<RentalOrder?> GetDetailAsync(Guid id, Guid currentUserId, bool isAdmin = false);
    Task<List<RentalOrder>> GetCustomerOrdersAsync(Guid customerId);
    Task<List<RentalOrder>> GetOwnerOrdersAsync(Guid ownerId);
    Task<RentalOrderOperationResult> ConfirmAsync(Guid id, Guid ownerId);
    Task<RentalOrderOperationResult> CancelAsync(Guid id, Guid currentUserId, string? reason);
    Task<RentalOrderOperationResult> SignContractAsync(Guid id, Guid currentUserId);
    Task<RentalOrderOperationResult> SelectPaymentAsync(Guid id, PhuongThucThanhToanDon method, Guid customerId);
    Task<RentalOrderOperationResult> MarkPaidAsync(Guid id, string reference, decimal? receivedAmount = null, string? gatewayNote = null);
    Task<RentalOrderOperationResult> ConfirmCashAsync(Guid id, Guid ownerId);
    Task<RentalOrderOperationResult> HandOverAsync(Guid id, Guid ownerId);
    Task<RentalOrderOperationResult> CompleteAsync(Guid id, Guid ownerId);
    Task<OwnerDashboardViewModel> GetOwnerDashboardAsync(Guid ownerId);
    Task<List<RentalCalendarEventViewModel>> GetCalendarAsync(Guid ownerId, DateTime? from, DateTime? to);
}

public class RentalOrderService : IRentalOrderService
{
    private readonly AppDbContext _db;
    private readonly ICartService _cart;

    public RentalOrderService(AppDbContext db, ICartService cart)
    {
        _db = db;
        _cart = cart;
    }

    public async Task<CheckoutViewModel> BuildCheckoutAsync(Guid customerId)
    {
        var customer = await _db.Users.AsNoTracking().FirstAsync(u => u.Id == customerId);
        var cart = await _cart.GetAsync();
        return new CheckoutViewModel
        {
            HoTen = customer.HoTen,
            Email = customer.Email,
            SDT = customer.SDT ?? string.Empty,
            SoCCCD = customer.SoCCCD ?? string.Empty,
            SoGPLX = customer.SoGPLX ?? string.Empty,
            DiaChi = customer.DiaChi ?? string.Empty,
            Items = cart.Items.Select(i => new CheckoutItemInput
            {
                VehicleId = i.VehicleId,
                TenXe = i.TenXe,
                BienSo = i.BienSo,
                LoaiXe = i.LoaiXe,
                GiaThueMotNgay = i.GiaThueMotNgay,
                TinhTrang = "Sẵn sàng cho thuê"
            }).ToList()
        };
    }

    public async Task<RentalOrderOperationResult> CreateAsync(CheckoutViewModel model, Guid customerId)
    {
        var customer = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == customerId);
        if (customer is null || !customer.DuDieuKienGiaoDich)
            return new(false, "Bạn phải hoàn tất hồ sơ và được Admin xác thực trước khi thuê xe.");
        var cart = await _cart.GetAsync();
        var cartIds = cart.Items.Select(i => i.VehicleId).OrderBy(id => id).ToList();
        var inputIds = model.Items.Select(i => i.VehicleId).Distinct().OrderBy(id => id).ToList();
        if (cartIds.Count == 0 || !cartIds.SequenceEqual(inputIds))
            return new(false, "Giỏ hàng đã thay đổi. Vui lòng tải lại trang checkout.");

        var vehicles = await _db.Vehicles
            .Include(v => v.Owner)
            .Where(v => inputIds.Contains(v.Id))
            .ToListAsync();
        if (vehicles.Count != inputIds.Count) return new(false, "Có xe không còn tồn tại.");
        if (vehicles.Any(v => v.TrangThai != TrangThaiXe.SanSang))
            return new(false, "Có xe không còn sẵn sàng cho thuê.");
        if (vehicles.Select(v => v.OwnerId).Distinct().Count() != 1)
            return new(false, "Các xe trong một đơn phải thuộc cùng một chủ.");

        foreach (var item in model.Items)
        {
            if (item.NgayNhanXe < DateTime.Now.AddMinutes(-5))
                return new(false, $"Thời gian nhận xe {item.TenXe} phải ở tương lai.");
            if (item.NgayTraXe <= item.NgayNhanXe)
                return new(false, $"Thời gian trả xe {item.TenXe} phải sau thời gian nhận.");
            if ((item.NgayTraXe - item.NgayNhanXe).TotalDays > 365)
                return new(false, "Mỗi lượt thuê không được vượt quá 365 ngày.");
        }

        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        foreach (var item in model.Items)
        {
            if (await IsVehicleBusyAsync(item.VehicleId, item.NgayNhanXe, item.NgayTraXe))
            {
                var vehicle = vehicles.First(v => v.Id == item.VehicleId);
                return new(false, $"{vehicle.TenXe} đã có lịch trùng trong khoảng thời gian bạn chọn.");
            }
        }

        var order = new RentalOrder
        {
            MaDon = $"RT-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..7].ToUpperInvariant()}",
            CustomerId = customerId,
            OwnerId = vehicles[0].OwnerId,
            HoTenKhach = model.HoTen.Trim(),
            EmailKhach = model.Email.Trim(),
            SDTKhach = model.SDT.Trim(),
            SoCCCD = model.SoCCCD.Trim(),
            SoGPLX = model.SoGPLX.Trim(),
            DiaChiKhach = model.DiaChi.Trim(),
            GhiChu = model.GhiChu?.Trim()
        };

        foreach (var input in model.Items)
        {
            var vehicle = vehicles.First(v => v.Id == input.VehicleId);
            var rentalDays = Math.Max(1, (int)Math.Ceiling((input.NgayTraXe - input.NgayNhanXe).TotalDays));
            order.Items.Add(new RentalOrderItem
            {
                VehicleId = vehicle.Id,
                TenXe = vehicle.TenXe,
                BienSo = vehicle.BienSo,
                LoaiXe = vehicle.LoaiXe,
                TinhTrangLucDat = $"Sẵn sàng; năm {vehicle.Nam}; {vehicle.HopSo}; {vehicle.NhienLieu}",
                NgayNhanXe = input.NgayNhanXe,
                NgayTraXe = input.NgayTraXe,
                DonGiaNgay = vehicle.GiaThueMotNgay,
                SoNgayThue = rentalDays,
                ThanhTien = vehicle.GiaThueMotNgay * rentalDays
            });
        }

        var policy = await _db.SystemPolicies.AsNoTracking().FirstOrDefaultAsync(p => p.Id == 1) ?? new SystemPolicy();
        order.TienThue = order.Items.Sum(i => i.ThanhTien);
        order.TienDatCoc = Math.Round(order.TienThue * policy.TyLeDatCoc / 100, 0);
        order.PhiDichVu = Math.Round(order.TienThue * policy.TyLePhiDichVu / 100, 0);
        order.TongTien = order.TienThue + order.TienDatCoc + order.PhiDichVu;
        var contractNumber = $"HD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..7].ToUpperInvariant()}";
        order.Contract = new RentalOrderContract
        {
            SoHopDong = contractNumber,
            NoiDungHopDong = BuildContract(order, vehicles[0].Owner, contractNumber),
            HoTenChuXe = vehicles[0].Owner.HoTen,
            EmailChuXe = vehicles[0].Owner.Email,
            SDTChuXe = vehicles[0].Owner.SDT,
            SoCCCDChuXe = vehicles[0].Owner.SoCCCD,
            DiaChiChuXe = vehicles[0].Owner.DiaChi
        };

        _db.RentalOrders.Add(order);
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();
        _cart.Clear();
        return new(true, Order: order);
    }

    public async Task<RentalOrder?> GetDetailAsync(Guid id, Guid currentUserId, bool isAdmin = false)
    {
        var order = await OrderQuery().FirstOrDefaultAsync(o => o.Id == id);
        if (order is null) return null;
        return isAdmin || order.CustomerId == currentUserId || order.OwnerId == currentUserId ? order : null;
    }

    public Task<List<RentalOrder>> GetCustomerOrdersAsync(Guid customerId) =>
        OrderQuery().Where(o => o.CustomerId == customerId).OrderByDescending(o => o.CreatedAt).ToListAsync();

    public Task<List<RentalOrder>> GetOwnerOrdersAsync(Guid ownerId) =>
        OrderQuery().Where(o => o.OwnerId == ownerId).OrderByDescending(o => o.CreatedAt).ToListAsync();

    public async Task<RentalOrderOperationResult> ConfirmAsync(Guid id, Guid ownerId)
    {
        var owner = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == ownerId);
        if (owner is null || !owner.DuDieuKienGiaoDich)
            return new(false, "Bạn phải hoàn tất hồ sơ và được Admin xác thực trước khi nhận đơn cho thuê.");
        var order = await OrderQuery().FirstOrDefaultAsync(o => o.Id == id);
        if (order is null || order.OwnerId != ownerId) return new(false, "Không tìm thấy đơn hoặc bạn không có quyền.");
        if (order.TrangThai != TrangThaiDonThue.ChoChuXeXacNhan) return new(false, "Đơn không ở trạng thái chờ xác nhận.");
        foreach (var item in order.Items)
            if (await IsVehicleBusyAsync(item.VehicleId, item.NgayNhanXe, item.NgayTraXe, order.Id))
                return new(false, $"{item.TenXe} vừa phát sinh lịch trùng. Vui lòng huỷ đơn và liên hệ khách.");

        order.TrangThai = TrangThaiDonThue.DaXacNhan;
        order.ConfirmedAt = DateTime.UtcNow;
        order.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return new(true, Order: order);
    }

    public async Task<RentalOrderOperationResult> CancelAsync(Guid id, Guid currentUserId, string? reason)
    {
        var order = await OrderQuery().FirstOrDefaultAsync(o => o.Id == id);
        if (order is null || (order.CustomerId != currentUserId && order.OwnerId != currentUserId))
            return new(false, "Không tìm thấy đơn hoặc bạn không có quyền.");
        if (order.TrangThai is TrangThaiDonThue.DangThue or TrangThaiDonThue.HoanThanh or TrangThaiDonThue.DaHuy)
            return new(false, "Không thể huỷ đơn ở trạng thái hiện tại.");
        if (order.TrangThaiThanhToan == TrangThaiThanhToanDon.DaThanhToan)
        {
            var cancellationPolicy = await _db.SystemPolicies.AsNoTracking().FirstOrDefaultAsync(p => p.Id == 1) ?? new SystemPolicy();
            var hoursBeforePickup = (order.Items.Min(i => i.NgayNhanXe) - DateTime.Now).TotalHours;
            order.TyLeHoanTien = hoursBeforePickup >= 72 ? cancellationPolicy.HoanTienTruoc72Gio
                : hoursBeforePickup >= 24 ? cancellationPolicy.HoanTienTruoc24Gio : cancellationPolicy.HoanTienTrong24Gio;
            order.SoTienHoanDuKien = Math.Round(order.TongTien * order.TyLeHoanTien / 100, 0);
            order.TrangThaiThanhToan = TrangThaiThanhToanDon.ChoHoanTien;
        }
        order.TrangThai = TrangThaiDonThue.DaHuy;
        order.LyDoHuy = reason?.Trim();
        order.CancelledAt = DateTime.UtcNow;
        order.GhiChu = string.Join(Environment.NewLine, new[] { order.GhiChu, $"Lý do huỷ: {reason}" }.Where(s => !string.IsNullOrWhiteSpace(s)));
        order.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return new(true, Order: order);
    }

    public async Task<RentalOrderOperationResult> SignContractAsync(Guid id, Guid currentUserId)
    {
        var order = await OrderQuery().FirstOrDefaultAsync(o => o.Id == id);
        if (order?.Contract is null || (order.CustomerId != currentUserId && order.OwnerId != currentUserId))
            return new(false, "Không tìm thấy hợp đồng hoặc bạn không có quyền.");
        if (order.TrangThai == TrangThaiDonThue.DaHuy) return new(false, "Đơn đã huỷ.");

        if (order.CustomerId == currentUserId)
        {
            if (order.Contract.DaKyKhach) return new(false, "Khách thuê đã ký hợp đồng.");
            order.Contract.DaKyKhach = true;
            order.Contract.NgayKyKhach = DateTime.UtcNow;
        }
        else
        {
            if (order.Contract.DaKyChuXe) return new(false, "Chủ xe đã ký hợp đồng.");
            order.Contract.DaKyChuXe = true;
            order.Contract.NgayKyChuXe = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync();
        return new(true, Order: order);
    }

    public async Task<RentalOrderOperationResult> SelectPaymentAsync(Guid id, PhuongThucThanhToanDon method, Guid customerId)
    {
        var order = await OrderQuery().FirstOrDefaultAsync(o => o.Id == id);
        if (order is null || order.CustomerId != customerId) return new(false, "Không tìm thấy đơn hoặc bạn không có quyền.");
        if (order.TrangThai != TrangThaiDonThue.DaXacNhan && order.TrangThai != TrangThaiDonThue.ChoThanhToan)
            return new(false, "Chủ xe cần xác nhận đơn trước khi thanh toán.");
        if (order.Contract is null || !order.Contract.DaKyKhach || !order.Contract.DaKyChuXe)
            return new(false, "Cả khách thuê và chủ xe phải ký hợp đồng trước khi thanh toán.");
        if (order.TrangThaiThanhToan == TrangThaiThanhToanDon.DaThanhToan)
            return new(false, "Đơn đã thanh toán.");

        order.PhuongThucThanhToan = method;
        order.TrangThai = TrangThaiDonThue.ChoThanhToan;
        order.TrangThaiThanhToan = method == PhuongThucThanhToanDon.ThanhToanKhiNhanXe
            ? TrangThaiThanhToanDon.ChoThuTienKhiNhanXe : TrangThaiThanhToanDon.ChoThanhToan;

        var payment = new RentalOrderPayment
        {
            RentalOrderId = order.Id,
            MaGiaoDich = $"PAY-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}",
            PhuongThuc = method,
            TrangThai = order.TrangThaiThanhToan,
            SoTien = order.TongTien
        };
        order.MaThamChieuThanhToan = payment.MaGiaoDich;
        _db.RentalOrderPayments.Add(payment);
        await _db.SaveChangesAsync();
        return new(true, Order: order);
    }

    public async Task<RentalOrderOperationResult> MarkPaidAsync(Guid id, string reference, decimal? receivedAmount = null, string? gatewayNote = null)
    {
        var order = await OrderQuery().FirstOrDefaultAsync(o => o.Id == id);
        if (order is null) return new(false, "Không tìm thấy đơn.");
        if (!string.Equals(order.MaThamChieuThanhToan, reference, StringComparison.OrdinalIgnoreCase))
            return new(false, "Mã tham chiếu thanh toán không khớp.");
        if (receivedAmount.HasValue && receivedAmount.Value != order.TongTien)
            return new(false, $"Số tiền nhận được không khớp tổng đơn ({receivedAmount.Value:N0}/{order.TongTien:N0} đ).");
        if (order.TrangThaiThanhToan == TrangThaiThanhToanDon.DaThanhToan)
            return new(true, Order: order);

        var payment = order.Payments.OrderByDescending(p => p.CreatedAt)
            .FirstOrDefault(p => p.MaGiaoDich == reference);
        if (payment is null) return new(false, "Không tìm thấy giao dịch.");
        payment.TrangThai = TrangThaiThanhToanDon.DaThanhToan;
        payment.PaidAt = DateTime.UtcNow;
        payment.GhiChuCongThanhToan = gatewayNote;
        order.TrangThaiThanhToan = TrangThaiThanhToanDon.DaThanhToan;
        order.TrangThai = TrangThaiDonThue.DaThanhToan;
        order.PaidAt = DateTime.UtcNow;
        order.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return new(true, Order: order);
    }

    public async Task<RentalOrderOperationResult> ConfirmCashAsync(Guid id, Guid ownerId)
    {
        var order = await OrderQuery().FirstOrDefaultAsync(o => o.Id == id);
        if (order is null || order.OwnerId != ownerId) return new(false, "Không tìm thấy đơn hoặc bạn không có quyền.");
        if (order.TrangThaiThanhToan != TrangThaiThanhToanDon.ChoThuTienKhiNhanXe)
            return new(false, "Đơn không chọn thanh toán khi nhận xe.");
        return await MarkPaidAsync(id, order.MaThamChieuThanhToan!, order.TongTien, "Chủ xe xác nhận đã thu tiền khi giao xe.");
    }

    public async Task<RentalOrderOperationResult> HandOverAsync(Guid id, Guid ownerId)
    {
        var order = await OrderQuery().FirstOrDefaultAsync(o => o.Id == id);
        if (order is null || order.OwnerId != ownerId) return new(false, "Không tìm thấy đơn hoặc bạn không có quyền.");
        if (order.TrangThaiThanhToan != TrangThaiThanhToanDon.DaThanhToan)
            return new(false, "Cần hoàn tất thanh toán trước khi giao xe.");
        order.TrangThai = TrangThaiDonThue.DangThue;
        foreach (var item in order.Items) item.Vehicle.TrangThai = TrangThaiXe.DangThue;
        order.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return new(true, Order: order);
    }

    public async Task<RentalOrderOperationResult> CompleteAsync(Guid id, Guid ownerId)
    {
        var order = await OrderQuery().FirstOrDefaultAsync(o => o.Id == id);
        if (order is null || order.OwnerId != ownerId) return new(false, "Không tìm thấy đơn hoặc bạn không có quyền.");
        if (order.TrangThai != TrangThaiDonThue.DangThue) return new(false, "Đơn chưa ở trạng thái đang thuê.");
        order.TrangThai = TrangThaiDonThue.HoanThanh;
        foreach (var item in order.Items) item.Vehicle.TrangThai = TrangThaiXe.SanSang;
        order.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return new(true, Order: order);
    }

    public async Task<OwnerDashboardViewModel> GetOwnerDashboardAsync(Guid ownerId)
    {
        var now = DateTime.UtcNow;
        var startToday = now.Date;
        var startMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var startYear = new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var paid = _db.RentalOrderPayments.AsNoTracking()
            .Where(p => p.RentalOrder.OwnerId == ownerId && p.TrangThai == TrangThaiThanhToanDon.DaThanhToan);

        var recentPayments = await paid.Where(p => p.PaidAt >= now.AddMonths(-11).Date)
            .Select(p => new { p.PaidAt, p.SoTien }).ToListAsync();

        return new OwnerDashboardViewModel
        {
            DoanhThuHomNay = await paid.Where(p => p.PaidAt >= startToday).SumAsync(p => (decimal?)p.SoTien) ?? 0,
            DoanhThuThangNay = await paid.Where(p => p.PaidAt >= startMonth).SumAsync(p => (decimal?)p.SoTien) ?? 0,
            DoanhThuNamNay = await paid.Where(p => p.PaidAt >= startYear).SumAsync(p => (decimal?)p.SoTien) ?? 0,
            DonChoXacNhan = await _db.RentalOrders.CountAsync(o => o.OwnerId == ownerId && o.TrangThai == TrangThaiDonThue.ChoChuXeXacNhan),
            XeDangThue = await _db.Vehicles.CountAsync(v => v.OwnerId == ownerId && v.TrangThai == TrangThaiXe.DangThue),
            DoanhThu12Thang = Enumerable.Range(0, 12).Select(i => now.AddMonths(-11 + i)).Select(m => new OwnerRevenuePoint
            {
                Label = m.ToString("MM/yyyy"),
                Revenue = recentPayments.Where(p => p.PaidAt!.Value.Year == m.Year && p.PaidAt.Value.Month == m.Month).Sum(p => p.SoTien)
            }).ToList(),
            DonGanDay = await OrderQuery().Where(o => o.OwnerId == ownerId).OrderByDescending(o => o.CreatedAt).Take(8).ToListAsync()
        };
    }

    public async Task<List<RentalCalendarEventViewModel>> GetCalendarAsync(Guid ownerId, DateTime? from, DateTime? to)
    {
        var start = from ?? DateTime.Today.AddMonths(-1);
        var end = to ?? DateTime.Today.AddMonths(3);
        return await _db.RentalOrderItems.AsNoTracking()
            .Where(i => i.RentalOrder.OwnerId == ownerId
                && i.RentalOrder.TrangThai != TrangThaiDonThue.DaHuy
                && i.NgayNhanXe < end && i.NgayTraXe > start)
            .Select(i => new RentalCalendarEventViewModel
            {
                Title = i.TenXe + " · " + i.BienSo + " · " + i.RentalOrder.HoTenKhach,
                Start = i.NgayNhanXe.ToString("yyyy-MM-ddTHH:mm:ss"),
                End = i.NgayTraXe.ToString("yyyy-MM-ddTHH:mm:ss"),
                Color = i.RentalOrder.TrangThai == TrangThaiDonThue.ChoChuXeXacNhan ? "#E2A33D" :
                    i.RentalOrder.TrangThai == TrangThaiDonThue.DangThue ? "#2F6F8F" : "#1F6F52",
                Url = "/RentalOrder/Details/" + i.RentalOrderId
            }).ToListAsync();
    }

    private IQueryable<RentalOrder> OrderQuery() => _db.RentalOrders
        .Include(o => o.Customer)
        .Include(o => o.Owner)
        .Include(o => o.Items).ThenInclude(i => i.Vehicle).ThenInclude(v => v.HinhAnh)
        .Include(o => o.Contract)
        .Include(o => o.Payments);

    private async Task<bool> IsVehicleBusyAsync(Guid vehicleId, DateTime start, DateTime end, Guid? excludedOrderId = null)
    {
        var startDate = DateOnly.FromDateTime(start);
        var endDate = DateOnly.FromDateTime(end);
        var legacyBusy = await _db.Bookings.AnyAsync(b => b.VehicleId == vehicleId
            && b.TrangThai != TrangThaiBooking.DaHuy
            && b.NgayBatDau <= endDate && b.NgayKetThuc >= startDate);
        if (legacyBusy) return true;

        return await _db.RentalOrderItems.AnyAsync(i => i.VehicleId == vehicleId
            && (excludedOrderId == null || i.RentalOrderId != excludedOrderId)
            && i.RentalOrder.TrangThai != TrangThaiDonThue.DaHuy
            && i.NgayNhanXe < end && i.NgayTraXe > start);
    }

    private static string BuildContract(RentalOrder order, User owner, string contractNumber)
    {
        var sb = new StringBuilder();
        sb.AppendLine("CỘNG HÒA XÃ HỘI CHỦ NGHĨA VIỆT NAM");
        sb.AppendLine("Độc lập - Tự do - Hạnh phúc");
        sb.AppendLine();
        sb.AppendLine($"HỢP ĐỒNG THUÊ XE ĐIỆN TỬ SỐ {contractNumber}");
        sb.AppendLine($"Mã đơn: {order.MaDon} · Ngày lập: {DateTime.Now:dd/MM/yyyy HH:mm}");
        sb.AppendLine();
        sb.AppendLine("BÊN CHO THUÊ (BÊN A)");
        sb.AppendLine($"Họ tên: {owner.HoTen}");
        sb.AppendLine($"CCCD/CMND: {owner.SoCCCD ?? "(chưa cập nhật)"}");
        sb.AppendLine($"Email: {owner.Email} · Điện thoại: {owner.SDT ?? "(chưa cập nhật)"}");
        sb.AppendLine($"Địa chỉ: {owner.DiaChi ?? "(chưa cập nhật)"}");
        sb.AppendLine();
        sb.AppendLine("BÊN THUÊ (BÊN B)");
        sb.AppendLine($"Họ tên: {order.HoTenKhach}");
        sb.AppendLine($"CCCD/CMND: {order.SoCCCD} · GPLX: {order.SoGPLX}");
        sb.AppendLine($"Email: {order.EmailKhach} · Điện thoại: {order.SDTKhach}");
        sb.AppendLine($"Địa chỉ: {order.DiaChiKhach}");
        sb.AppendLine();
        sb.AppendLine("DANH SÁCH XE VÀ THỜI GIAN THUÊ");
        var index = 1;
        foreach (var item in order.Items)
        {
            sb.AppendLine($"{index++}. {item.TenXe} · {item.LoaiXe} · Biển số {item.BienSo}");
            sb.AppendLine($"   Tình trạng: {item.TinhTrangLucDat}");
            sb.AppendLine($"   Nhận: {item.NgayNhanXe:dd/MM/yyyy HH:mm} · Trả: {item.NgayTraXe:dd/MM/yyyy HH:mm}");
            sb.AppendLine($"   {item.SoNgayThue} ngày × {item.DonGiaNgay:N0} VNĐ = {item.ThanhTien:N0} VNĐ");
        }
        sb.AppendLine();
        sb.AppendLine($"TIỀN THUÊ: {order.TienThue:N0} VNĐ · ĐẶT CỌC: {order.TienDatCoc:N0} VNĐ · PHÍ DỊCH VỤ: {order.PhiDichVu:N0} VNĐ");
        sb.AppendLine($"TỔNG GIÁ TRỊ HỢP ĐỒNG: {order.TongTien:N0} VNĐ");
        sb.AppendLine();
        sb.AppendLine("ĐIỀU KHOẢN");
        sb.AppendLine("1. Bên A bàn giao đúng xe, giấy tờ và tình trạng đã mô tả; bảo đảm xe đủ điều kiện vận hành.");
        sb.AppendLine("2. Bên B sử dụng xe đúng mục đích, đúng pháp luật; không chuyển giao cho người khác khi chưa được Bên A đồng ý.");
        sb.AppendLine("3. Bên B kiểm tra và xác nhận tình trạng xe khi nhận, hoàn trả đúng thời gian và tình trạng, trừ hao mòn tự nhiên.");
        sb.AppendLine("4. Chi phí nhiên liệu, phạt vi phạm giao thông và thiệt hại do lỗi của Bên B trong thời gian thuê do Bên B chịu.");
        sb.AppendLine("5. Việc huỷ, hoàn tiền và bồi thường được hai bên xử lý theo trạng thái thanh toán và thỏa thuận có lưu vết trên hệ thống.");
        sb.AppendLine("6. Hợp đồng có hiệu lực khi hai bên ký điện tử; dữ liệu đơn, thời gian ký và giao dịch là bằng chứng thực hiện hợp đồng.");
        return sb.ToString();
    }
}
