using Maintenance.Api.Contracts;
using Maintenance.Application.Auth;
using Microsoft.AspNetCore.Mvc;

namespace Maintenance.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
[Produces("application/json")]
public sealed class AuthController(AuthService auth) : ControllerBase
{
    /// <summary>Creates an account. A verification email follows (from slice S10).</summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MessageResponse>> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        await auth.RegisterAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, new MessageResponse("Account created. Check your email to verify the address."));
    }
}
