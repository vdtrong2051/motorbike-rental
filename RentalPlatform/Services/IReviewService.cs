using RentalPlatform.Models.Entities;
using RentalPlatform.Models.ViewModels.Review;

namespace RentalPlatform.Services;

public class ReviewOperationResult
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public Review? Review { get; init; }

    public static ReviewOperationResult Ok(Review review) => new() { Success = true, Review = review };
    public static ReviewOperationResult Fail(string error) => new() { Success = false, Error = error };
}

public interface IReviewService
{
    // Customer tạo đánh giá cho 1 Booking đã HoanThanh, mỗi Booking chỉ đánh giá được 1 lần
    Task<ReviewOperationResult> CreateAsync(ReviewCreateViewModel model, Guid customerId);

    Task<List<Review>> GetByVehicleAsync(Guid vehicleId);
    Task<(double TrungBinh, int SoLuong)> GetThongKeAsync(Guid vehicleId);
}
