using FluentAssertions;
using Maintenance.Api.Auth;
using Maintenance.Application.Time;
using Maintenance.Domain.Users;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using NSubstitute;

namespace Maintenance.UnitTests.Auth;

public class JwtTokenIssuerTests
{
    private static readonly DateTime Now = new(2026, 9, 8, 12, 0, 0, DateTimeKind.Utc);

    private readonly JwtOptions _options = new()
    {
        SigningKey = "unit-test-signing-key-with-32-plus-characters",
        Lifetime = TimeSpan.FromHours(24),
    };

    private JwtTokenIssuer CreateIssuer()
    {
        var clock = Substitute.For<IClock>();
        clock.UtcNow.Returns(Now);
        return new JwtTokenIssuer(Options.Create(_options), clock);
    }

    [Fact]
    public async Task Issue_ProducesATokenThatValidatesWithTheSameKey_AndCarriesSubAndEmailVerified()
    {
        var user = User.Create("someone@example.com", "hash", Now);
        user.MarkEmailVerified();

        var session = CreateIssuer().Issue(user);

        var result = await new JsonWebTokenHandler().ValidateTokenAsync(session.Token, new TokenValidationParameters
        {
            ValidIssuer = _options.Issuer,
            ValidAudience = _options.Audience,
            IssuerSigningKey = _options.SecurityKey,
            ValidateLifetime = false,
        });
        result.IsValid.Should().BeTrue();
        result.Claims[JwtRegisteredClaimNames.Sub].Should().Be(user.Id.ToString());
        result.Claims[JwtTokenIssuer.EmailVerifiedClaim].Should().Be("true");
        session.ExpiresAt.Should().Be(Now.AddHours(24));
    }

    [Fact]
    public async Task Issue_WithAnotherKey_DoesNotValidate()
    {
        var session = CreateIssuer().Issue(User.Create("someone@example.com", "hash", Now));

        var result = await new JsonWebTokenHandler().ValidateTokenAsync(session.Token, new TokenValidationParameters
        {
            ValidIssuer = _options.Issuer,
            ValidAudience = _options.Audience,
            IssuerSigningKey = new JwtOptions { SigningKey = "a-different-signing-key-with-32-plus-chars" }.SecurityKey,
            ValidateLifetime = false,
        });

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_RejectsShortKeys()
    {
        var act = () => new JwtOptions { SigningKey = "short" }.Validate();

        act.Should().Throw<InvalidOperationException>().WithMessage("*user-secrets*");
    }
}
