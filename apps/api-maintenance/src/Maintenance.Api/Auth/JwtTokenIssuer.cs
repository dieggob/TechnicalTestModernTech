using System.Security.Claims;
using Maintenance.Application.Auth;
using Maintenance.Application.Time;
using Maintenance.Domain.Users;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Maintenance.Api.Auth;

/// <summary>
/// Issues HS256-signed JWTs with the user id as <c>sub</c> and an <c>email_verified</c> claim.
/// The only type that knows how sessions are signed; validation is the JwtBearer middleware's.
/// </summary>
public sealed class JwtTokenIssuer(IOptions<JwtOptions> options, IClock clock) : ITokenIssuer
{
    public const string EmailVerifiedClaim = "email_verified";

    private readonly JsonWebTokenHandler _handler = new();

    public SessionToken Issue(User user)
    {
        var jwt = options.Value;
        var now = clock.UtcNow;
        var expiresAt = now.Add(jwt.Lifetime);

        var token = _handler.CreateToken(new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(EmailVerifiedClaim, user.EmailVerified ? "true" : "false"),
            ]),
            Issuer = jwt.Issuer,
            Audience = jwt.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = expiresAt,
            SigningCredentials = new SigningCredentials(jwt.SecurityKey, SecurityAlgorithms.HmacSha256),
        });

        return new SessionToken(token, expiresAt);
    }
}
