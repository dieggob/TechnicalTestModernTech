using FluentValidation;
using Maintenance.Application.Exceptions;
using Maintenance.Application.Observability;
using Maintenance.Application.Time;
using Maintenance.Application.Validation;
using Maintenance.Domain.Users;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Maintenance.Application.Auth;

/// <summary>
/// Account use cases. Each public method is one behaviour from the design's auth flows.
/// </summary>
public sealed class AuthService(
    IUserRepository users,
    IUserTokenRepository tokens,
    IPasswordHasher passwordHasher,
    ITokenGenerator tokenGenerator,
    IEmailSender emailSender,
    IClock clock,
    IOptions<TokenOptions> tokenOptions,
    IOptions<ClientOptions> clientOptions,
    IValidator<RegisterRequest> registerValidator,
    IMaintenanceMetrics metrics,
    ILogger<AuthService> logger)
{
    public async Task RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        ValidationRunner.Validate(registerValidator, request);

        if (await users.FindByEmailAsync(request.Email, cancellationToken) is not null)
        {
            throw new ConflictException("Email is already registered.");
        }

        var user = User.Create(request.Email, passwordHasher.Hash(request.Password), clock.UtcNow);
        await users.AddAsync(user, cancellationToken);
        var verification = await IssueTokenAsync(user, TokenPurpose.EmailVerification, cancellationToken);
        await users.SaveChangesAsync(cancellationToken);
        metrics.SignUp();

        await SendAsync(() => emailSender.SendVerificationAsync(user.Email, clientOptions.Value.VerifyEmailLink(verification.Raw), cancellationToken), user.Email);
    }

    /// <summary>
    /// Creates a token for the user, superseding any usable token of the same purpose, and stores
    /// only its hash. The caller saves; the raw value is returned for the email.
    /// </summary>
    private async Task<GeneratedToken> IssueTokenAsync(User user, TokenPurpose purpose, CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        await tokens.SupersedeAsync(user.Id, purpose, now, cancellationToken);
        var generated = tokenGenerator.Generate();
        await tokens.AddAsync(UserToken.Issue(user.Id, purpose, generated.Hash, now, tokenOptions.Value.Lifetime), cancellationToken);
        return generated;
    }

    /// <summary>Email hand-off failure never fails the request; the user can ask for a resend.</summary>
    private async Task SendAsync(Func<Task> send, string email)
    {
        try
        {
            await send();
            metrics.EmailSent();
        }
        catch (Exception exception)
        {
            metrics.EmailFailed();
            logger.LogError(exception, "Sending email to {Email} failed", email);
        }
    }
}
