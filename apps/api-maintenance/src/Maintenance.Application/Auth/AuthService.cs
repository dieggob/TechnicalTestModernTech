using FluentValidation;
using Maintenance.Application.Exceptions;
using Maintenance.Application.Observability;
using Maintenance.Application.Time;
using Maintenance.Application.Validation;
using Maintenance.Domain.Users;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ValidationException = Maintenance.Application.Exceptions.ValidationException;

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
    IValidator<VerifyEmailRequest> verifyValidator,
    IValidator<ResendVerificationRequest> resendValidator,
    IMaintenanceMetrics metrics,
    ILogger<AuthService> logger)
{
    private const string InvalidLinkMessage = "The link is invalid or has expired.";

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

        await SendVerificationAsync(user, verification);
    }

    public async Task VerifyEmailAsync(VerifyEmailRequest request, CancellationToken cancellationToken)
    {
        ValidationRunner.Validate(verifyValidator, request);

        var token = await ConsumeTokenAsync(request.Token, TokenPurpose.EmailVerification, cancellationToken);
        var user = await users.FindByIdAsync(token.UserId, cancellationToken)
            ?? throw new NotFoundException("User");

        user.MarkEmailVerified();
        await users.SaveChangesAsync(cancellationToken);
        metrics.Verification();
    }

    /// <summary>Answers the same way for unknown, verified, and unverified emails so nothing is revealed.</summary>
    public async Task ResendVerificationAsync(ResendVerificationRequest request, CancellationToken cancellationToken)
    {
        ValidationRunner.Validate(resendValidator, request);

        var user = await users.FindByEmailAsync(request.Email, cancellationToken);
        if (user is null || user.EmailVerified)
        {
            return;
        }

        var verification = await IssueTokenAsync(user, TokenPurpose.EmailVerification, cancellationToken);
        await users.SaveChangesAsync(cancellationToken);

        await SendVerificationAsync(user, verification);
    }

    /// <summary>
    /// Looks the presented token up by hash and consumes it when it is usable for the purpose;
    /// otherwise reports an invalid link without saying why (unknown, expired, used, or wrong purpose).
    /// </summary>
    private async Task<UserToken> ConsumeTokenAsync(string rawToken, TokenPurpose purpose, CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var token = await tokens.FindByHashAsync(tokenGenerator.Hash(rawToken), cancellationToken);
        if (token is null || !token.IsUsable(purpose, now))
        {
            throw new ValidationException(new Dictionary<string, string[]> { ["token"] = [InvalidLinkMessage] });
        }

        token.MarkUsed(now);
        return token;
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

    private Task SendVerificationAsync(User user, GeneratedToken verification) =>
        SendAsync(() => emailSender.SendVerificationAsync(user.Email, clientOptions.Value.VerifyEmailLink(verification.Raw), CancellationToken.None), user.Email);

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
