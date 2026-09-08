namespace Maintenance.Api.Hosting;

/// <summary>Configuration section "Cors": the one origin the browser client is served from.</summary>
public sealed class CorsSettings
{
    public const string Section = "Cors";

    public string ClientOrigin { get; set; } = "http://localhost:4200";
}
