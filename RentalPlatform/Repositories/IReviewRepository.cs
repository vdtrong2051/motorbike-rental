using RentalPlatform.Models.Entities;

namespace RentalPlatform.Repositories;

public interface IReviewRepository
{
    Task<Review?> GetByBookingIdAsync(Guid bookingId);
    Task<Review> AddAsync(Review review);
    Task<List<Review>> GetByVehicleAsync(Guid vehicleId);

    // Trả về (điểm trung bình, số lượng đánh giá) của 1 xe. (0, 0) nếu xe chưa có đánh giá nào.
    Task<(double TrungBinh, int SoLuong)> GetThongKeAsync(Guid vehicleId);
}
