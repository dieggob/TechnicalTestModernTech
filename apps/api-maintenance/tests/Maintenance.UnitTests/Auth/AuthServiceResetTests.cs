using FluentAssertions;
using Maintenance.Application.Auth;
using Maintenance.Application.Observability;
using Maintenance.Application.Time;
using Maintenance.Domain.Users;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Maintenance.UnitTests.Auth;

public class AuthServiceResetTests
{
    private static readonly DateTime Now = new(2026, 9, 8, 12, 0, 0, DateTimeKind.Utc);

    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IUserTokenRepository _tokens = Substitute.For<IUserTokenRepository>();
    private readonly ITokenGenerator _tokenGenerator = Substitute.For<ITokenGenerator>();
    private readonly IEmailSender _emailSender = Substitute.For<IEmailSender>();
    private readonly IMaintenanceMetrics _metrics = Substitute.For<IMaintenanceMetrics>();
    private readonly AuthService _service;
    private readonly User _user = User.Create("someone@example.com", "hash", Now);

    public AuthServiceResetTests()
    {
        var clock = Substitute.For<IClock>();
        clock.UtcNow.Returns(Now);
        _tokenGenerator.Generate().Returns(new GeneratedToken("reset-raw", "hash-of-reset"));
        _users.FindByEmailAsync(_user.Email, Arg.Any<CancellationToken>()).Returns(_user);
        _service = new AuthService(_users, _tokens, Substitute.For<IPasswordHasher>(), _tokenGenerator, Substitute.For<ITokenIssuer>(),
            _emailSender, clock, Options.Create(new TokenOptions()), Options.Create(new ClientOptions { BaseUrl = "http://client.test" }),
            new RegisterRequestValidator(), new VerifyEmailRequestValidator(), new ResendVerificationRequestValidator(),
            new LoginRequestValidator(), new ForgotPasswordRequestValidator(), new ResetPasswordRequestValidator(), _metrics, NullLogger<AuthService>.Instance);
    }

    [Fact]
    public async Task RequestReset_KnownEmail_IssuesResetToken_AndEmailsTheLink()
    {
        UserToken? issued = null;
        _tokens.AddAsync(Arg.Do<UserToken>(token => issued = token), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        await _service.RequestPasswordResetAsync(new ForgotPasswordRequest(_user.Email), CancellationToken.None);

        issued!.Purpose.Should().Be(TokenPurpose.PasswordReset);
        issued.TokenHash.Should().Be("hash-of-reset");
        await _tokens.Received(1).SupersedeAsync(_user.Id, TokenPurpose.PasswordReset, Now, Arg.Any<CancellationToken>());
        await _emailSender.Received(1).SendPasswordResetAsync(_user.Email, "http://client.test/reset-password?token=reset-raw", Arg.Any<CancellationToken>());
        _metrics.Received(1).ResetRequest();
    }

    [Fact]
    public async Task RequestReset_UnknownEmail_IssuesNothing_AndSendsNothing()
    {
        await _service.RequestPasswordResetAsync(new ForgotPasswordRequest("nobody@example.com"), CancellationToken.None);

        await _tokens.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
        await _emailSender.DidNotReceiveWithAnyArgs().SendPasswordResetAsync(default!, default!, default);
        _metrics.DidNotReceive().ResetRequest();
    }
}
