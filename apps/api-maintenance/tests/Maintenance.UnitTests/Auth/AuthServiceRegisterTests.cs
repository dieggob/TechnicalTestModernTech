using FluentAssertions;
using Maintenance.Application.Auth;
using Maintenance.Application.Exceptions;
using Maintenance.Application.Observability;
using Maintenance.Domain.Users;
using NSubstitute;

namespace Maintenance.UnitTests.Auth;

public class AuthServiceRegisterTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly IMaintenanceMetrics _metrics = Substitute.For<IMaintenanceMetrics>();
    private readonly AuthService _service;

    public AuthServiceRegisterTests()
    {
        _hasher.Hash(Arg.Any<string>()).Returns(call => "hashed:" + call.Arg<string>());
        _service = new AuthService(_users, _hasher, new RegisterRequestValidator(), _metrics);
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
    public async Task Register_ExistingEmail_ThrowsConflict()
    {
        _users.FindByEmailAsync("taken@example.com", Arg.Any<CancellationToken>())
            .Returns(User.Create("taken@example.com", "hash", DateTime.UtcNow));

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
