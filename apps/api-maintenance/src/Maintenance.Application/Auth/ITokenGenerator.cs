namespace Maintenance.Application.Auth;

/// <summary>A freshly generated one-time token: the raw value goes into the email, the hash into the database.</summary>
public sealed record GeneratedToken(string Raw, string Hash);

public interface ITokenGenerator
{
    GeneratedToken Generate();

    /// <summary>Hashes a raw token presented by a user so it can be looked up.</summary>
    string Hash(string raw);
}
