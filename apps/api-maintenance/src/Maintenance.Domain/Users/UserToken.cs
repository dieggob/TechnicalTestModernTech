namespace Maintenance.Domain.Users;

/// <summary>
/// A single-use, expiring token emailed to a user for one purpose. Only the hash of the
/// emailed value is stored. Owns the usability rules: purpose, expiry, and single use.
/// </summary>
public sealed class UserToken
{
    private UserToken()
    {
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public TokenPurpose Purpose { get; private set; }
    public string TokenHash { get; private set; } = null!;
    public DateTime ExpiresAt { get; private set; }
    public DateTime? UsedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public static UserToken Issue(Guid userId, TokenPurpose purpose, string tokenHash, DateTime now, TimeSpan lifetime) => new()
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        Purpose = purpose,
        TokenHash = tokenHash,
        ExpiresAt = now.Add(lifetime),
        CreatedAt = now,
    };

    public bool IsUsable(TokenPurpose purpose, DateTime now) =>
        Purpose == purpose && UsedAt is null && ExpiresAt > now;

    /// <summary>Consumes the token, whether by use or by supersession; the state is the same.</summary>
    public void MarkUsed(DateTime now) => UsedAt ??= now;
}
