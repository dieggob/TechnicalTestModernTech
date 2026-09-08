using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace Maintenance.Api.RateLimiting;

/// <summary>Configuration section "RateLimits".</summary>
public sealed class RateLimitOptions
{
    public const string Section = "RateLimits";

    /// <summary>Requests allowed per client address per window on login, resend, and forgot-password.</summary>
    public int AuthPermitLimit { get; set; } = 10;

    public TimeSpan AuthWindow { get; set; } = TimeSpan.FromMinutes(1);
}

/// <summary>
/// One fixed-window policy for the credential-related endpoints, partitioned by client address.
/// Applied per endpoint with <c>[EnableRateLimiting(AuthRateLimitPolicy.Name)]</c>.
/// </summary>
public static class AuthRateLimitPolicy
{
    public const string Name = "auth";

    public static void Configure(RateLimiterOptions options, RateLimitOptions limits)
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.AddPolicy(Name, context => RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = limits.AuthPermitLimit,
                Window = limits.AuthWindow,
                QueueLimit = 0,
            }));
    }
}
