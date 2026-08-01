using System.Text;
using RentalPlatform.Models.Entities;
using RentalPlatform.Repositories;

namespace RentalPlatform.Services;

public class ContractService : IContractService
{
    private readonly IContractRepository _contractRepository;
    private readonly IBookingRepository _bookingRepository;
    private readonly IVehicleRepository _vehicleRepository;

    public ContractService(
        IContractRepository contractRepository,
        IBookingRepository bookingRepository,
        IVehicleRepository vehicleRepository)
    {
        _contractRepository = contractRepository;
        _bookingRepository = bookingRepository;
        _vehicleRepository = vehicleRepository;
    }

    public async Task<Contract> TaoHopDongNeuChuaCoAsync(Guid bookingId)
    {
        var existing = await _contractRepository.GetByBookingIdAsync(bookingId);
        if (existing is not null) return existing;

        var booking = await _bookingRepository.GetByIdAsync(bookingId)
            ?? throw new InvalidOperationException("Không tìm thấy booking để sinh hợp đồng.");

        // Cần Vehicle kèm Owner đầy đủ để đưa vào nội dung hợp đồng
        var vehicle = await _vehicleRepository.GetByIdAsync(booking.VehicleId, includeImages: false)
            ?? throw new InvalidOperationException("Không tìm thấy xe để sinh hợp đồng.");

        var soHopDong = await SinhSoHopDongDuyNhatAsync();

        var contract = new Contract
        {
            BookingId = booking.Id,
            SoHopDong = soHopDong,
            NoiDungHopDong = SinhNoiDungHopDong(booking, vehicle, soHopDong)
        };

        return await _contractRepository.AddAsync(contract);
    }

    public async Task<ContractOperationResult> KyHopDongAsync(Guid bookingId, Guid currentUserId)
    {
        var contract = await _contractRepository.GetByBookingIdAsync(bookingId);
        if (contract is null) return ContractOperationResult.Fail("Chưa có hợp đồng cho đơn đặt xe này.");

        var laKhach = contract.Booking.CustomerId == currentUserId;
        var laChuXe = contract.Booking.Vehicle.OwnerId == currentUserId;

        if (!laKhach && !laChuXe)
            return ContractOperationResult.Fail("Bạn không có quyền ký hợp đồng này.");

        if (laKhach)
        {
            if (contract.DaKyKhach) return ContractOperationResult.Fail("Bạn đã ký hợp đồng này rồi.");
            contract.DaKyKhach = true;
            contract.NgayKyKhach = DateTime.UtcNow;
        }
        else
        {
            if (contract.DaKyChuXe) return ContractOperationResult.Fail("Bạn đã ký hợp đồng này rồi.");
            contract.DaKyChuXe = true;
            contract.NgayKyChuXe = DateTime.UtcNow;
        }

        await _contractRepository.UpdateAsync(contract);
        return ContractOperationResult.Ok(contract);
    }

    public async Task<Contract?> GetByBookingIdAsync(Guid bookingId, Guid currentUserId, bool isAdmin)
    {
        var contract = await _contractRepository.GetByBookingIdAsync(bookingId);
        if (contract is null) return null;
        if (!isAdmin && contract.Booking.CustomerId != currentUserId && contract.Booking.Vehicle.OwnerId != currentUserId)
            return null;
        return contract;
    }

    private async Task<string> SinhSoHopDongDuyNhatAsync()
    {
        string soHopDong;
        do
        {
            soHopDong = $"HD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
        } while (await _contractRepository.SoHopDongExistsAsync(soHopDong));

        return soHopDong;
    }

    private static string SinhNoiDungHopDong(Booking booking, Vehicle vehicle, string soHopDong)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"HỢP ĐỒNG THUÊ XE ĐIỆN TỬ SỐ {soHopDong}");
        sb.AppendLine($"Ngày lập: {DateTime.UtcNow:dd/MM/yyyy HH:mm}");
        sb.AppendLine();
        sb.AppendLine("BÊN CHO THUÊ (Bên A):");
        sb.AppendLine($"- Họ tên: {vehicle.Owner.HoTen}");
        sb.AppendLine($"- Email: {vehicle.Owner.Email}");
        sb.AppendLine($"- Số điện thoại: {vehicle.Owner.SDT ?? "(chưa cập nhật)"}");
        sb.AppendLine();
        sb.AppendLine("BÊN THUÊ (Bên B):");
        sb.AppendLine($"- Họ tên: {booking.Customer.HoTen}");
        sb.AppendLine($"- Email: {booking.Customer.Email}");
        sb.AppendLine($"- Số điện thoại: {booking.Customer.SDT ?? "(chưa cập nhật)"}");
        sb.AppendLine();
        sb.AppendLine("THÔNG TIN XE THUÊ:");
        sb.AppendLine($"- Tên xe: {vehicle.TenXe} ({vehicle.LoaiXe})");
        sb.AppendLine($"- Biển số: {vehicle.BienSo}");
        sb.AppendLine($"- Số chỗ ngồi: {vehicle.SoChoNgoi}");
        sb.AppendLine();
        sb.AppendLine("THỜI GIAN VÀ GIÁ THUÊ:");
        sb.AppendLine($"- Ngày nhận xe: {booking.NgayBatDau:dd/MM/yyyy}");
        sb.AppendLine($"- Ngày trả xe: {booking.NgayKetThuc:dd/MM/yyyy}");
        sb.AppendLine($"- Đơn giá: {booking.GiaThueMotNgayLucDat:N0} đ/ngày");
        sb.AppendLine($"- Tổng tiền thuê: {booking.TongTien:N0} đ");
        sb.AppendLine();
        sb.AppendLine("ĐIỀU KHOẢN CƠ BẢN:");
        sb.AppendLine("1. Bên B có trách nhiệm đặt cọc và thanh toán đầy đủ trước khi nhận xe.");
        sb.AppendLine("2. Bên B có trách nhiệm trả xe đúng ngày, đúng tình trạng đã nhận (trừ hao mòn tự nhiên).");
        sb.AppendLine("3. Bên A có trách nhiệm bàn giao xe đúng ngày, đảm bảo xe trong tình trạng vận hành tốt.");
        sb.AppendLine("4. Mọi hư hỏng phát sinh do lỗi của Bên B trong thời gian thuê sẽ do Bên B chịu trách nhiệm bồi thường theo thoả thuận.");
        sb.AppendLine("5. Hợp đồng có hiệu lực khi cả hai bên xác nhận ký điện tử trên hệ thống.");

        return sb.ToString();
    }
}
