namespace Maintenance.Application.Auth;

/// <summary>
/// The authenticated caller. The only way services learn who is calling; controllers never pass
/// a user id from the request body or path.
/// </summary>
public interface ICurrentUser
{
    /// <summary>The caller's user id. Throws when the request is not authenticated.</summary>
    Guid UserId { get; }
}
