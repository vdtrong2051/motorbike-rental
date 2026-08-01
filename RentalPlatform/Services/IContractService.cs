using RentalPlatform.Models.Entities;

namespace RentalPlatform.Services;

public class ContractOperationResult
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public Contract? Contract { get; init; }

    public static ContractOperationResult Ok(Contract contract) => new() { Success = true, Contract = contract };
    public static ContractOperationResult Fail(string error) => new() { Success = false, Error = error };
}

public interface IContractService
{
    // Sinh hợp đồng tự động từ thông tin Booking (chỉ tạo nếu chưa có), gọi bởi
    // BookingService ngay sau khi Owner xác nhận đơn.
    Task<Contract> TaoHopDongNeuChuaCoAsync(Guid bookingId);

    Task<ContractOperationResult> KyHopDongAsync(Guid bookingId, Guid currentUserId);

    Task<Contract?> GetByBookingIdAsync(Guid bookingId, Guid currentUserId, bool isAdmin);
}
