using System.Security.Claims;
using Maintenance.Application.Auth;
using Maintenance.Application.Exceptions;

namespace Maintenance.Api.Auth;

/// <summary>Reads the caller's id from the validated bearer token's claims.</summary>
public sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public Guid UserId => TryGetUserId(httpContextAccessor.HttpContext?.User) ?? throw new UnauthorizedException();

    /// <summary>JwtBearer maps the token's <c>sub</c> claim to <see cref="ClaimTypes.NameIdentifier"/>.</summary>
    public static Guid? TryGetUserId(ClaimsPrincipal? principal) =>
        Guid.TryParse(principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
}
