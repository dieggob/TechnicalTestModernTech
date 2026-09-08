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

public class AuthServiceLoginTests
{
    private static readonly DateTime Now = new(2026, 9, 8, 12, 0, 0, DateTimeKind.Utc);

    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly ITokenIssuer _issuer = Substitute.For<ITokenIssuer>();
    private readonly IMaintenanceMetrics _metrics = Substitute.For<IMaintenanceMetrics>();
    private readonly AuthService _service;
    private readonly User _user = User.Create("someone@example.com", "stored-hash", Now);

    public AuthServiceLoginTests()
    {
        var clock = Substitute.For<IClock>();
        clock.UtcNow.Returns(Now);
        _users.FindByEmailAsync(_user.Email, Arg.Any<CancellationToken>()).Returns(_user);
        _hasher.Verify("Secret123", "stored-hash").Returns(true);
        _issuer.Issue(_user).Returns(new SessionToken("jwt", Now.AddHours(24)));
        _service = new AuthService(_users, Substitute.For<IUserTokenRepository>(), _hasher, Substitute.For<ITokenGenerator>(),
            _issuer, Substitute.For<IEmailSender>(), clock, Options.Create(new TokenOptions()), Options.Create(new ClientOptions()),
            new RegisterRequestValidator(), new VerifyEmailRequestValidator(), new ResendVerificationRequestValidator(),
            new LoginRequestValidator(), new ForgotPasswordRequestValidator(), new ResetPasswordRequestValidator(), _metrics, NullLogger<AuthService>.Instance);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsTokenWithVerificationFlag()
    {
        var result = await _service.LoginAsync(new LoginRequest("someone@example.com", "Secret123"), CancellationToken.None);

        result.Token.Should().Be("jwt");
        result.UserId.Should().Be(_user.Id);
        result.EmailVerified.Should().BeFalse();
        result.ExpiresAt.Should().Be(Now.AddHours(24));
        _metrics.Received(1).Login();
    }

    [Fact]
    public async Task Login_UnknownEmail_ThrowsUnauthorized_AndCountsFailure()
    {
        var act = () => _service.LoginAsync(new LoginRequest("nobody@example.com", "Secret123"), CancellationToken.None);

        (await act.Should().ThrowAsync<UnauthorizedException>()).WithMessage("Invalid email or password.");
        _metrics.Received(1).FailedLogin();
        _issuer.DidNotReceiveWithAnyArgs().Issue(default!);
    }

    [Fact]
    public async Task Login_WrongPassword_ThrowsTheSameUnauthorized()
    {
        var act = () => _service.LoginAsync(new LoginRequest(_user.Email, "Wrong999"), CancellationToken.None);

        (await act.Should().ThrowAsync<UnauthorizedException>()).WithMessage("Invalid email or password.");
        _metrics.Received(1).FailedLogin();
    }

    [Fact]
    public async Task Login_MissingPassword_ThrowsValidation()
    {
        var act = () => _service.LoginAsync(new LoginRequest(_user.Email, ""), CancellationToken.None);

        (await act.Should().ThrowAsync<ValidationException>()).Which.Errors.Should().ContainKey("password");
    }
}
