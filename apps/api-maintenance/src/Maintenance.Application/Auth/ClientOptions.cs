namespace Maintenance.Application.Auth;

/// <summary>Configuration section "Client": where the web client is reachable, for emailed links.</summary>
public sealed class ClientOptions
{
    public const string Section = "Client";

    public string BaseUrl { get; set; } = "http://localhost:4200";

    public string VerifyEmailLink(string rawToken) => $"{BaseUrl.TrimEnd('/')}/verify?token={Uri.EscapeDataString(rawToken)}";

    public string ResetPasswordLink(string rawToken) => $"{BaseUrl.TrimEnd('/')}/reset-password?token={Uri.EscapeDataString(rawToken)}";
}
