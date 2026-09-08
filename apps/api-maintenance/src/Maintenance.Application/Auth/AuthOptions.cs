namespace Maintenance.Application.Auth;

/// <summary>Configuration section "Auth".</summary>
public sealed class AuthOptions
{
    public const string Section = "Auth";

    /// <summary>
    /// Product decision: all features are available without email verification by default.
    /// When true, every endpoint outside /auth answers 403 EmailNotVerified for unverified accounts.
    /// </summary>
    public bool RequireEmailVerification { get; set; }
}
