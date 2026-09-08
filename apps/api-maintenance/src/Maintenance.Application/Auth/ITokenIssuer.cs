using Maintenance.Domain.Users;

namespace Maintenance.Application.Auth;

/// <summary>A signed session token and when it stops being accepted.</summary>
public sealed record SessionToken(string Token, DateTime ExpiresAt);

/// <summary>
/// Issues stateless session tokens carrying the user id and verification flag.
/// Validation is done by the API's authentication middleware, not through this interface.
/// </summary>
public interface ITokenIssuer
{
    SessionToken Issue(User user);
}
