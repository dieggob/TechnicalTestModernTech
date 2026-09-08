namespace Maintenance.Domain.Users;

/// <summary>
/// An account. Owns its creation invariants: the email is normalised once here, and a new
/// account always starts unverified.
/// </summary>
public sealed class User
{
    private User()
    {
    }

    public Guid Id { get; private set; }
    public string Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public bool EmailVerified { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public static User Create(string email, string passwordHash, DateTime now) => new()
    {
        Id = Guid.NewGuid(),
        Email = NormalizeEmail(email),
        PasswordHash = passwordHash,
        EmailVerified = false,
        CreatedAt = now,
    };

    /// <summary>Trimmed and lower-cased, so lookups and the unique index agree on one form.</summary>
    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    /// <summary>Verification happens once; there is no transition back.</summary>
    public void MarkEmailVerified() => EmailVerified = true;

    public void ChangePassword(string newPasswordHash) => PasswordHash = newPasswordHash;
}
