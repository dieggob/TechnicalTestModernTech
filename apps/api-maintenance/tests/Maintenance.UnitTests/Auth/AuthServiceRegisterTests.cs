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

public class AuthServiceRegisterTests
{
    private static readonly DateTime Now = new(2026, 9, 8, 12, 0, 0, DateTimeKind.Utc);

    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IUserTokenRepository _tokens = Substitute.For<IUserTokenRepository>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly ITokenGenerator _tokenGenerator = Substitute.For<ITokenGenerator>();
    private readonly IEmailSender _emailSender = Substitute.For<IEmailSender>();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly IMaintenanceMetrics _metrics = Substitute.For<IMaintenanceMetrics>();
    private readonly AuthService _service;

    public AuthServiceRegisterTests()
    {
        _hasher.Hash(Arg.Any<string>()).Returns(call => "hashed:" + call.Arg<string>());
        _tokenGenerator.Generate().Returns(new GeneratedToken("raw-token", "hash-of-raw-token"));
        _clock.UtcNow.Returns(Now);
        _service = new AuthService(_users, _tokens, _hasher, _tokenGenerator, Substitute.For<ITokenIssuer>(), _emailSender, _clock,
            Options.Create(new TokenOptions { Lifetime = TimeSpan.FromMinutes(30) }),
            Options.Create(new ClientOptions { BaseUrl = "http://client.test" }),
            new RegisterRequestValidator(), new VerifyEmailRequestValidator(), new ResendVerificationRequestValidator(),
            new LoginRequestValidator(), new ForgotPasswordRequestValidator(), _metrics, NullLogger<AuthService>.Instance);
    }

    [Fact]
    public async Task Register_NewEmail_StoresHashedPasswordAndNormalisedEmail()
    {
        User? added = null;
        _users.AddAsync(Arg.Do<User>(user => added = user), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        await _service.RegisterAsync(new RegisterRequest("  Diego@Example.com ", "Secret123"), CancellationToken.None);

        added.Should().NotBeNull();
        added!.Email.Should().Be("diego@example.com");
        added.PasswordHash.Should().Be("hashed:Secret123");
        added.EmailVerified.Should().BeFalse();
        await _users.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        _metrics.Received(1).SignUp();
    }

    [Fact]
    public async Task Register_IssuesVerificationToken_StoringOnlyTheHash_AndEmailsTheRawLink()
    {
        UserToken? issued = null;
        _tokens.AddAsync(Arg.Do<UserToken>(token => issued = token), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        await _service.RegisterAsync(new RegisterRequest("new@example.com", "Secret123"), CancellationToken.None);

        issued.Should().NotBeNull();
        issued!.Purpose.Should().Be(TokenPurpose.EmailVerification);
        issued.TokenHash.Should().Be("hash-of-raw-token");
        issued.ExpiresAt.Should().Be(Now.AddMinutes(30));
        await _emailSender.Received(1).SendVerificationAsync("new@example.com",
            "http://client.test/verify?token=raw-token", Arg.Any<CancellationToken>());
        _metrics.Received(1).EmailSent();
    }

    [Fact]
    public async Task Register_WhenEmailSendingFails_StillSucceeds_AndCountsTheFailure()
    {
        _emailSender.SendVerificationAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("smtp down")));

        var act = () => _service.RegisterAsync(new RegisterRequest("new@example.com", "Secret123"), CancellationToken.None);

        await act.Should().NotThrowAsync();
        await _users.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        _metrics.Received(1).EmailFailed();
    }

    [Fact]
    public async Task Register_ExistingEmail_ThrowsConflict()
    {
        _users.FindByEmailAsync("taken@example.com", Arg.Any<CancellationToken>())
            .Returns(User.Create("taken@example.com", "hash", Now));

        var act = () => _service.RegisterAsync(new RegisterRequest("taken@example.com", "Secret123"), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        await _users.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("not-an-email", "Secret123", "email")]
    [InlineData("ok@example.com", "short1", "password")]
    [InlineData("ok@example.com", "lettersonly", "password")]
    public async Task Register_InvalidInput_ThrowsValidationWithFieldError(string email, string password, string field)
    {
        var act = () => _service.RegisterAsync(new RegisterRequest(email, password), CancellationToken.None);

        (await act.Should().ThrowAsync<ValidationException>()).Which.Errors.Should().ContainKey(field);
        await _users.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }
}
