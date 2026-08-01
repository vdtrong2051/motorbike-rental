using RentalPlatform.Models.Entities;
using RentalPlatform.Models.ViewModels.Review;
using RentalPlatform.Repositories;

namespace RentalPlatform.Services;

public class ReviewService : IReviewService
{
    private readonly IReviewRepository _reviewRepository;
    private readonly IBookingRepository _bookingRepository;

    public ReviewService(IReviewRepository reviewRepository, IBookingRepository bookingRepository)
    {
        _reviewRepository = reviewRepository;
        _bookingRepository = bookingRepository;
    }

    public async Task<ReviewOperationResult> CreateAsync(ReviewCreateViewModel model, Guid customerId)
    {
        var booking = await _bookingRepository.GetByIdAsync(model.BookingId);
        if (booking is null) return ReviewOperationResult.Fail("Không tìm thấy đơn đặt xe.");
        if (booking.CustomerId != customerId) return ReviewOperationResult.Fail("Bạn không có quyền đánh giá đơn này.");
        if (booking.TrangThai != TrangThaiBooking.HoanThanh)
            return ReviewOperationResult.Fail("Chỉ có thể đánh giá sau khi chuyến đi đã hoàn thành.");

        var daDanhGia = await _reviewRepository.GetByBookingIdAsync(booking.Id);
        if (daDanhGia is not null)
            return ReviewOperationResult.Fail("Bạn đã đánh giá đơn này rồi.");

        var review = new Review
        {
            BookingId = booking.Id,
            VehicleId = booking.VehicleId,
            CustomerId = customerId,
            SoSao = model.SoSao,
            NhanXet = model.NhanXet
        };

        review = await _reviewRepository.AddAsync(review);
        return ReviewOperationResult.Ok(review);
    }

    public Task<List<Review>> GetByVehicleAsync(Guid vehicleId) => _reviewRepository.GetByVehicleAsync(vehicleId);

    public Task<(double TrungBinh, int SoLuong)> GetThongKeAsync(Guid vehicleId) => _reviewRepository.GetThongKeAsync(vehicleId);
}
