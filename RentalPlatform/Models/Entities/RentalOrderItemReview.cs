using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RentalPlatform.Models.Entities;

/// <summary>
/// GD3: one review per completed RentalOrderItem. The older Review/Booking schema is untouched.
/// SQL bootstrap: db/gd3_order_item_reviews.sql (no EF migration).
/// </summary>
public sealed class RentalOrderItemReview
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid RentalOrderItemId { get; set; }
    [ForeignKey(nameof(RentalOrderItemId))]
    public RentalOrderItem RentalOrderItem { get; set; } = null!;

    public Guid VehicleId { get; set; }
    [ForeignKey(nameof(VehicleId))]
    public Vehicle Vehicle { get; set; } = null!;

    public Guid CustomerId { get; set; }
    [ForeignKey(nameof(CustomerId))]
    public User Customer { get; set; } = null!;

    [Range(1, 5)]
    public int SoSao { get; set; }

    [MaxLength(1000)]
    public string? NhanXet { get; set; }

    public bool IsVisible { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
