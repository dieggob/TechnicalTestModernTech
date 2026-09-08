namespace Maintenance.Application.Auth;

/// <summary>Login response: the session token plus what the client needs to render the shell.</summary>
public sealed record AuthResult(string Token, Guid UserId, bool EmailVerified, DateTime ExpiresAt);
