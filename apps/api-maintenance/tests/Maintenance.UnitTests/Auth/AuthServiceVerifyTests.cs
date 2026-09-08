using FluentAssertions;
using Maintenance.Application.Auth;
using Maintenance.Application.Exceptions;
using Maintenance.Application.Observability;
using Maintenance.Application.Time;
using Maintenance.Domain.Users;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Maintenance.UnitTests.Auth;

public class AuthServiceVerifyTests
{
    private static readonly DateTime Now = new(2026, 9, 8, 12, 0, 0, DateTimeKind.Utc);

    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IUserTokenRepository _tokens = Substitute.For<IUserTokenRepository>();
    private readonly ITokenGenerator _tokenGenerator = Substitute.For<ITokenGenerator>();
    private readonly IEmailSender _emailSender = Substitute.For<IEmailSender>();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly IMaintenanceMetrics _metrics = Substitute.For<IMaintenanceMetrics>();
    private readonly AuthService _service;
    private readonly User _user = User.Create("someone@example.com", "hash", Now);

    public AuthServiceVerifyTests()
    {
        _clock.UtcNow.Returns(Now);
        _tokenGenerator.Hash("raw").Returns("hash-of-raw");
        _tokenGenerator.Generate().Returns(new GeneratedToken("fresh", "hash-of-fresh"));
        _users.FindByIdAsync(_user.Id, Arg.Any<CancellationToken>()).Returns(_user);
        _users.FindByEmailAsync(_user.Email, Arg.Any<CancellationToken>()).Returns(_user);
        _service = new AuthService(_users, _tokens, Substitute.For<IPasswordHasher>(), _tokenGenerator, Substitute.For<ITokenIssuer>(), _emailSender, _clock,
            Options.Create(new TokenOptions()), Options.Create(new ClientOptions()),
            new RegisterRequestValidator(), new VerifyEmailRequestValidator(), new ResendVerificationRequestValidator(),
            new LoginRequestValidator(), _metrics, NullLogger<AuthService>.Instance);
    }

    private void TokenExists(TokenPurpose purpose, DateTime issuedAt) =>
        _tokens.FindByHashAsync("hash-of-raw", Arg.Any<CancellationToken>())
            .Returns(UserToken.Issue(_user.Id, purpose, "hash-of-raw", issuedAt, TimeSpan.FromMinutes(30)));

    [Fact]
    public async Task Verify_UsableToken_MarksUserVerified_AndConsumesToken()
    {
        TokenExists(TokenPurpose.EmailVerification, Now);

        await _service.VerifyEmailAsync(new VerifyEmailRequest("raw"), CancellationToken.None);

        _user.EmailVerified.Should().BeTrue();
        await _users.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        _metrics.Received(1).Verification();
    }

    [Fact]
    public async Task Verify_TokenForAnotherPurpose_IsRejected()
    {
        TokenExists(TokenPurpose.PasswordReset, Now);

        var act = () => _service.VerifyEmailAsync(new VerifyEmailRequest("raw"), CancellationToken.None);

        (await act.Should().ThrowAsync<ValidationException>()).Which.Errors.Should().ContainKey("token");
        _user.EmailVerified.Should().BeFalse();
    }

    [Fact]
    public async Task Verify_ExpiredToken_IsRejected()
    {
        TokenExists(TokenPurpose.EmailVerification, Now.AddMinutes(-31));

        var act = () => _service.VerifyEmailAsync(new VerifyEmailRequest("raw"), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Resend_ForUnverifiedUser_SupersedesOlderTokens_AndEmailsAFreshLink()
    {
        await _service.ResendVerificationAsync(new ResendVerificationRequest(_user.Email), CancellationToken.None);

        await _tokens.Received(1).SupersedeAsync(_user.Id, TokenPurpose.EmailVerification, Now, Arg.Any<CancellationToken>());
        await _emailSender.Received(1).SendVerificationAsync(_user.Email, Arg.Is<string>(link => link.Contains("token=fresh")), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Resend_ForVerifiedUser_DoesNothing()
    {
        _user.MarkEmailVerified();

        await _service.ResendVerificationAsync(new ResendVerificationRequest(_user.Email), CancellationToken.None);

        await _tokens.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
        await _emailSender.DidNotReceiveWithAnyArgs().SendVerificationAsync(default!, default!, default);
    }
}
