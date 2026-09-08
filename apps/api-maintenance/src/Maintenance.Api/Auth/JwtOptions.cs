using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Maintenance.Api.Auth;

/// <summary>Configuration section "Jwt". The signing key comes from user secrets or the environment, never appsettings.json.</summary>
public sealed class JwtOptions
{
    public const string Section = "Jwt";
    public const int MinimumKeyLength = 32;

    public string SigningKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = "maintenance-api";
    public string Audience { get; set; } = "maintenance-client";
    public TimeSpan Lifetime { get; set; } = TimeSpan.FromHours(24);

    public SymmetricSecurityKey SecurityKey => new(Encoding.UTF8.GetBytes(SigningKey));

    /// <summary>HS256 needs at least 256 bits; fail at start-up rather than at the first login.</summary>
    public void Validate()
    {
        if (SigningKey.Length < MinimumKeyLength)
        {
            throw new InvalidOperationException(
                $"Jwt:SigningKey must be at least {MinimumKeyLength} characters. Set it with " +
                "`dotnet user-secrets set Jwt:SigningKey <value>` (scripts/setup.sh does this) or the Jwt__SigningKey environment variable.");
        }
    }
}
