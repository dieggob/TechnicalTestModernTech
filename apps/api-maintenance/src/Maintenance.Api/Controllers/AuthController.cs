using Maintenance.Api.Contracts;
using Maintenance.Api.RateLimiting;
using Maintenance.Application.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Maintenance.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
[AllowAnonymous]
[Produces("application/json")]
public sealed class AuthController(AuthService auth) : ControllerBase
{
    /// <summary>Creates an account and emails a verification link.</summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MessageResponse>> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        await auth.RegisterAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, new MessageResponse("Account created. Check your email to verify the address."));
    }

    /// <summary>Verifies the email address behind an emailed link. Each link works once.</summary>
    [HttpPost("verify-email")]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MessageResponse>> VerifyEmail(VerifyEmailRequest request, CancellationToken cancellationToken)
    {
        await auth.VerifyEmailAsync(request, cancellationToken);
        return Ok(new MessageResponse("Email address verified."));
    }

    /// <summary>Sends a fresh verification link. The response is the same for any email.</summary>
    [HttpPost("resend-verification")]
    [EnableRateLimiting(AuthRateLimitPolicy.Name)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<MessageResponse>> ResendVerification(ResendVerificationRequest request, CancellationToken cancellationToken)
    {
        await auth.ResendVerificationAsync(request, cancellationToken);
        return Accepted(new MessageResponse("If that address needs verification, a new link is on its way."));
    }

    /// <summary>Exchanges credentials for a session token. Verification does not gate login.</summary>
    [HttpPost("login")]
    [EnableRateLimiting(AuthRateLimitPolicy.Name)]
    [ProducesResponseType(typeof(AuthResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<AuthResult>> Login(LoginRequest request, CancellationToken cancellationToken) =>
        Ok(await auth.LoginAsync(request, cancellationToken));

    /// <summary>Emails a password-reset link to a registered address. The response is the same for any email.</summary>
    [HttpPost("forgot-password")]
    [EnableRateLimiting(AuthRateLimitPolicy.Name)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<MessageResponse>> ForgotPassword(ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        await auth.RequestPasswordResetAsync(request, cancellationToken);
        return Accepted(new MessageResponse("If that address is registered, a reset link is on its way."));
    }

    /// <summary>Sets a new password behind an emailed reset link. Each link works once; log in afterwards.</summary>
    [HttpPost("reset-password")]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MessageResponse>> ResetPassword(ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        await auth.ResetPasswordAsync(request, cancellationToken);
        return Ok(new MessageResponse("Password changed. You can log in with it now."));
    }
}
