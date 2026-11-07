using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace RentalPlatform.Services.Payments;

/// <summary>
/// Time-limited, authenticated bearer capability for deliberately simulated payments.
/// Does NOT grant real-world payment authority. DataProtection keys must be persisted
/// in multi-instance deployments; see docs/GD1_PAYMENT_CORE.md.
/// </summary>
public interface IDemoPaymentTokenService
{
    bool Enabled { get; }
    string Issue(Guid orderId, string reference, DateTime expiresAtUtc);
    bool TryRead(string token, out Guid orderId, out string reference);
}

public sealed class DemoPaymentTokenService : IDemoPaymentTokenService
{
    private readonly ITimeLimitedDataProtector _protector;
    public bool Enabled { get; }

    public DemoPaymentTokenService(IDataProtectionProvider protectionProvider,
        IConfiguration configuration, IHostEnvironment environment)
    {
        _protector = protectionProvider.CreateProtector("RentalPlatform.GD1.DemoPayment.v1")
            .ToTimeLimitedDataProtector();
        // Default: only development. Production requires an explicit opt-in.
        Enabled = environment.IsDevelopment() || configuration.GetValue<bool>("Payment:DemoEnabled");
    }

    public string Issue(Guid orderId, string reference, DateTime expiresAtUtc)
    {
        if (!Enabled) throw new InvalidOperationException("Demo payment is disabled.");
        var lifetime = expiresAtUtc - DateTime.UtcNow;
        if (lifetime <= TimeSpan.Zero) throw new InvalidOperationException("Payment session expired.");
        return _protector.Protect($"{orderId:N}|{reference}", lifetime);
    }

    public bool TryRead(string token, out Guid orderId, out string reference)
    {
        orderId = Guid.Empty;
        reference = string.Empty;
        if (!Enabled || string.IsNullOrWhiteSpace(token) || token.Length > 4096) return false;
        try
        {
            var values = _protector.Unprotect(token).Split('|', 2);
            if (values.Length != 2 || !Guid.TryParseExact(values[0], "N", out orderId)
                || string.IsNullOrWhiteSpace(values[1]) || values[1].Length > 60)
                return false;
            reference = values[1];
            return true;
        }
        catch (CryptographicException) { return false; }
        catch (FormatException) { return false; }
    }
}
