using System.ComponentModel.DataAnnotations;

namespace RentalPlatform.Models.Entities;

public enum UserRole
{
    Customer,
    Owner,
    Admin
}

public class User
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(100)]
    public string HoTen { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(15)]
    public string? SDT { get; set; }

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    public UserRole Role { get; set; } = UserRole.Customer;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Chỉ có ý nghĩa khi Role = Owner
    public ICollection<Vehicle> XeSoHuu { get; set; } = new List<Vehicle>();

    // Chỉ có ý nghĩa khi Role = Customer
    public ICollection<Booking> DonDaDat { get; set; } = new List<Booking>();
}