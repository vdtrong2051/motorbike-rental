using Microsoft.EntityFrameworkCore;
using RentalPlatform.Data;
using RentalPlatform.Models.Entities;
using RentalPlatform.Models.ViewModels.Review;

namespace RentalPlatform.Services;

public sealed record OrderReviewEligibility(RentalOrderItem? Item, bool Allowed, string? Error);
public sealed record OrderReviewResult(bool Success, string? Error = null, RentalOrderItemReview? Review = null);
public sealed record AdminOrderReviewList(
    List<RentalOrderItemReview> OrderReviews,
    List<Review> LegacyReviews,
    int? Stars);

public interface IOrderReviewService
{
    Task<OrderReviewEligibility> CheckEligibilityAsync(Guid itemId, Guid customerId);
    Task<OrderReviewResult> CreateAsync(OrderReviewCreateViewModel model, Guid customerId);
    Task<Dictionary<Guid, RentalOrderItemReview>> GetByOrderAsync(Guid orderId);
    Task<List<RentalOrderItemReview>> GetPublicByVehicleAsync(Guid vehicleId);
    Task<List<RentalOrderItemReview>> GetOwnerReviewsAsync(Guid ownerId);
    Task<AdminOrderReviewList> GetAdminReviewsAsync(int? stars);
    Task<bool> SetVisibilityAsync(Guid reviewId, bool visible, Guid adminId);
}

/// <summary>
/// Review policy for RentalOrderItem. Legacy Booking reviews stay in ReviewService.
/// All ownership, vehicle IDs and completed state are derived from persisted entities.
/// </summary>
public sealed class OrderReviewService : IOrderReviewService
{
    private readonly AppDbContext _db;
    public OrderReviewService(AppDbContext db) => _db = db;

    public async Task<OrderReviewEligibility> CheckEligibilityAsync(Guid itemId, Guid customerId)
    {
        var item = await _db.RentalOrderItems.AsNoTracking()
            .Include(i => i.RentalOrder)
            .Include(i => i.Vehicle)
            .FirstOrDefaultAsync(i => i.Id == itemId && i.RentalOrder.CustomerId == customerId);
        if (item is null) return new(null, false, "Không tìm thấy xe trong đơn thuê của bạn.");
        if (item.RentalOrder.TrangThai != TrangThaiDonThue.HoanThanh)
            return new(item, false, "Chỉ được đánh giá sau khi đơn thuê hoàn thành.");
        if (await _db.RentalOrderItemReviews.AsNoTracking().AnyAsync(r => r.RentalOrderItemId == itemId))
            return new(item, false, "Xe trong lần thuê này đã được đánh giá.");
        return new(item, true, null);
    }

    public async Task<OrderReviewResult> CreateAsync(OrderReviewCreateViewModel model, Guid customerId)
    {
        if (model.RentalOrderItemId == Guid.Empty)
            return new(false, "Thiếu thông tin xe trong đơn thuê.");
        if (model.SoSao is < 1 or > 5)
            return new(false, "Số sao phải từ 1 đến 5.");
        var content = model.NhanXet?.Trim();
        if (content?.Length > 1000)
            return new(false, "Nhận xét tối đa 1.000 ký tự.");

        var eligibility = await CheckEligibilityAsync(model.RentalOrderItemId, customerId);
        if (!eligibility.Allowed || eligibility.Item is null)
            return new(false, eligibility.Error);

        var review = new RentalOrderItemReview
        {
            RentalOrderItemId = eligibility.Item.Id,
            VehicleId = eligibility.Item.VehicleId,
            CustomerId = customerId,
            SoSao = model.SoSao,
            NhanXet = string.IsNullOrWhiteSpace(content) ? null : content,
            IsVisible = true,
            CreatedAt = DateTime.UtcNow
        };
        _db.RentalOrderItemReviews.Add(review);
        try
        {
            // Unique index on RentalOrderItemId handles concurrent submissions atomically.
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (
            ex.InnerException?.Message.Contains("Duplicate entry", StringComparison.OrdinalIgnoreCase) == true)
        {
            return new(false, "Xe trong lần thuê này đã được đánh giá.");
        }
        return new(true, Review: review);
    }

    public async Task<Dictionary<Guid, RentalOrderItemReview>> GetByOrderAsync(Guid orderId)
        => await _db.RentalOrderItemReviews.AsNoTracking()
            .Where(r => r.RentalOrderItem.RentalOrderId == orderId)
            .ToDictionaryAsync(r => r.RentalOrderItemId);

    public Task<List<RentalOrderItemReview>> GetPublicByVehicleAsync(Guid vehicleId)
        => _db.RentalOrderItemReviews.AsNoTracking()
            .Include(r => r.Customer)
            .Where(r => r.VehicleId == vehicleId && r.IsVisible)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

    public Task<List<RentalOrderItemReview>> GetOwnerReviewsAsync(Guid ownerId)
        => _db.RentalOrderItemReviews.AsNoTracking()
            .Include(r => r.Customer)
            .Include(r => r.Vehicle)
            .Include(r => r.RentalOrderItem).ThenInclude(i => i.RentalOrder)
            .Where(r => r.Vehicle.OwnerId == ownerId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

    public async Task<AdminOrderReviewList> GetAdminReviewsAsync(int? stars)
    {
        if (stars is < 1 or > 5) stars = null;
        var current = _db.RentalOrderItemReviews.AsNoTracking()
            .Include(r => r.Customer)
            .Include(r => r.Vehicle)
            .Include(r => r.RentalOrderItem).ThenInclude(i => i.RentalOrder)
            .AsQueryable();
        var legacy = _db.Reviews.AsNoTracking()
            .Include(r => r.Customer)
            .Include(r => r.Vehicle)
            .AsQueryable();
        if (stars.HasValue)
        {
            current = current.Where(r => r.SoSao == stars.Value);
            legacy = legacy.Where(r => r.SoSao == stars.Value);
        }
        return new(
            await current.OrderByDescending(r => r.CreatedAt).ToListAsync(),
            await legacy.OrderByDescending(r => r.CreatedAt).ToListAsync(), stars);
    }

    public async Task<bool> SetVisibilityAsync(Guid reviewId, bool visible, Guid adminId)
    {
        var authorized = await _db.Users.AsNoTracking()
            .AnyAsync(u => u.Id == adminId && u.Role == UserRole.Admin && !u.BiTamKhoa);
        if (!authorized) return false;
        var review = await _db.RentalOrderItemReviews.FirstOrDefaultAsync(r => r.Id == reviewId);
        if (review is null) return false;
        if (review.IsVisible != visible)
        {
            review.IsVisible = visible;
            await _db.SaveChangesAsync();
        }
        return true;
    }
}
