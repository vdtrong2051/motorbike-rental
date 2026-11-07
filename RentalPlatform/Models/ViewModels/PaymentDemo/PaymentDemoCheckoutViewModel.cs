namespace RentalPlatform.Models.ViewModels.PaymentDemo;

public sealed class PaymentDemoCheckoutViewModel
{
    public bool IsValid { get; init; }
    public string Message { get; init; } = string.Empty;
    public string Token { get; init; } = string.Empty;
    public string OrderCode { get; init; } = string.Empty;
    public string Reference { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string Method { get; init; } = string.Empty;
    public DateTime ExpiresAtUtc { get; init; }
}
