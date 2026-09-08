namespace Maintenance.Application.Auth;

/// <summary>
/// Slow, salted password hashing. Implemented in Infrastructure so the algorithm can change
/// without touching services.
/// </summary>
public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string password, string passwordHash);
}
