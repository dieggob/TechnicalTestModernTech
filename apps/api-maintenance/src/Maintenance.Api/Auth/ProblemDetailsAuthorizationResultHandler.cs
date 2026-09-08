using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Mvc;

namespace Maintenance.Api.Auth;

/// <summary>
/// Gives the verification-gate failure a ProblemDetails body with a machine-readable code, so the
/// client can route to the verification screen. Every other authorization outcome keeps the default
/// behaviour (401 challenge, plain 403).
/// </summary>
public sealed class ProblemDetailsAuthorizationResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _default = new();

    public async Task HandleAsync(RequestDelegate next, HttpContext context, AuthorizationPolicy policy, PolicyAuthorizationResult authorizeResult)
    {
        var emailNotVerified = authorizeResult.Forbidden
            && authorizeResult.AuthorizationFailure?.FailureReasons.Any(reason => reason.Message == EmailVerifiedRequirement.FailureCode) == true;

        if (!emailNotVerified)
        {
            await _default.HandleAsync(next, context, policy, authorizeResult);
            return;
        }

        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status403Forbidden,
            Title = EmailVerifiedRequirement.FailureMessage,
            Extensions = { ["code"] = EmailVerifiedRequirement.FailureCode },
        };
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        await context.Response.WriteAsJsonAsync(problem);
    }
}
