using RentalPlatform.Models.Entities;
using RentalPlatform.Models.ViewModels.Payment;
using RentalPlatform.Repositories;

namespace RentalPlatform.Services;

public class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IBookingRepository _bookingRepository;
    private readonly IContractRepository _contractRepository;

    public decimal TyLeDatCoc => 0.3m; // Đặt cọc 30% tổng tiền, mô phỏng theo thông lệ phổ biến

    public PaymentService(
        IPaymentRepository paymentRepository,
        IBookingRepository bookingRepository,
        IContractRepository contractRepository)
    {
        _paymentRepository = paymentRepository;
        _bookingRepository = bookingRepository;
        _contractRepository = contractRepository;
    }

    public async Task<PaymentOperationResult> DatCocAsync(PaymentCreateViewModel model, Guid currentUserId)
    {
        var booking = await _bookingRepository.GetByIdAsync(model.BookingId);
        if (booking is null) return PaymentOperationResult.Fail("Không tìm thấy đơn đặt xe.");
        if (booking.CustomerId != currentUserId) return PaymentOperationResult.Fail("Bạn không có quyền thanh toán cho đơn này.");
        if (booking.TrangThai != TrangThaiBooking.DaXacNhan)
            return PaymentOperationResult.Fail("Chỉ có thể đặt cọc khi đơn đã được chủ xe xác nhận.");

        var contract = await _contractRepository.GetByBookingIdAsync(booking.Id);
        if (contract is null || !contract.DaKyKhach || !contract.DaKyChuXe)
            return PaymentOperationResult.Fail("Cả hai bên cần ký hợp đồng điện tử trước khi đặt cọc.");

        if (await _paymentRepository.DaCoLoaiThanhToanAsync(booking.Id, LoaiThanhToan.DatCoc))
            return PaymentOperationResult.Fail("Đơn này đã được đặt cọc trước đó.");

        var payment = new Payment
        {
            BookingId = booking.Id,
            LoaiThanhToan = LoaiThanhToan.DatCoc,
            PhuongThuc = model.PhuongThuc,
            SoTien = Math.Round(booking.TongTien * TyLeDatCoc, 0),
            MaGiaoDich = SinhMaGiaoDich(),
            TrangThai = TrangThaiThanhToan.ThanhCong
        };

        payment = await _paymentRepository.AddAsync(payment);
        return PaymentOperationResult.Ok(payment);
    }

    public async Task<PaymentOperationResult> ThanhToanConLaiAsync(PaymentCreateViewModel model, Guid currentUserId)
    {
        var booking = await _bookingRepository.GetByIdAsync(model.BookingId);
        if (booking is null) return PaymentOperationResult.Fail("Không tìm thấy đơn đặt xe.");
        if (booking.CustomerId != currentUserId) return PaymentOperationResult.Fail("Bạn không có quyền thanh toán cho đơn này.");
        if (booking.TrangThai != TrangThaiBooking.DaXacNhan)
            return PaymentOperationResult.Fail("Chỉ có thể thanh toán phần còn lại khi đơn đã được chủ xe xác nhận.");

        if (!await _paymentRepository.DaCoLoaiThanhToanAsync(booking.Id, LoaiThanhToan.DatCoc))
            return PaymentOperationResult.Fail("Cần đặt cọc trước khi thanh toán phần còn lại.");

        if (await _paymentRepository.DaCoLoaiThanhToanAsync(booking.Id, LoaiThanhToan.ThanhToanConLai))
            return PaymentOperationResult.Fail("Đơn này đã được thanh toán đủ trước đó.");

        var daThanhToan = await _paymentRepository.GetTongDaThanhToanAsync(booking.Id);
        var conLai = booking.TongTien - daThanhToan;
        if (conLai <= 0) return PaymentOperationResult.Fail("Đơn này đã được thanh toán đủ.");

        var payment = new Payment
        {
            BookingId = booking.Id,
            LoaiThanhToan = LoaiThanhToan.ThanhToanConLai,
            PhuongThuc = model.PhuongThuc,
            SoTien = conLai,
            MaGiaoDich = SinhMaGiaoDich(),
            TrangThai = TrangThaiThanhToan.ThanhCong
        };

        payment = await _paymentRepository.AddAsync(payment);
        return PaymentOperationResult.Ok(payment);
    }

    public Task<List<Payment>> GetByBookingIdAsync(Guid bookingId) => _paymentRepository.GetByBookingIdAsync(bookingId);

    public Task<decimal> GetTongDaThanhToanAsync(Guid bookingId) => _paymentRepository.GetTongDaThanhToanAsync(bookingId);

    private static string SinhMaGiaoDich() => $"GD{DateTime.UtcNow:yyyyMMddHHmmss}{Random.Shared.Next(1000, 9999)}";
}
