using Maintenance.Application.Auth;
using Maintenance.Domain.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Maintenance.Api.Auth;

/// <summary>
/// Part of the fallback policy on every endpoint outside /auth. Satisfied for everyone while
/// <see cref="AuthOptions.RequireEmailVerification"/> is off; when on, only for accounts whose
/// current stored <c>EmailVerified</c> is true, so verifying mid-session takes effect at once.
/// </summary>
public sealed class EmailVerifiedRequirement : IAuthorizationRequirement
{
    public const string FailureCode = "EmailNotVerified";
    public const string FailureMessage = "Email address not verified.";
}

public sealed class EmailVerifiedHandler(IOptions<AuthOptions> options, IUserRepository users)
    : AuthorizationHandler<EmailVerifiedRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, EmailVerifiedRequirement requirement)
    {
        if (!options.Value.RequireEmailVerification)
        {
            context.Succeed(requirement);
            return;
        }

        var userId = CurrentUser.TryGetUserId(context.User);
        var user = userId is null ? null : await users.FindByIdAsync(userId.Value, CancellationToken.None);
        if (user is { EmailVerified: true })
        {
            context.Succeed(requirement);
        }
        else
        {
            context.Fail(new AuthorizationFailureReason(this, EmailVerifiedRequirement.FailureCode));
        }
    }
}
