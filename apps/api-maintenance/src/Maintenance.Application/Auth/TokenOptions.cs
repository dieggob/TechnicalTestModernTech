namespace Maintenance.Application.Auth;

/// <summary>Configuration section "Tokens".</summary>
public sealed class TokenOptions
{
    public const string Section = "Tokens";

    /// <summary>How long verification and reset links stay valid. Product decision: 30 minutes.</summary>
    public TimeSpan Lifetime { get; set; } = TimeSpan.FromMinutes(30);
}
