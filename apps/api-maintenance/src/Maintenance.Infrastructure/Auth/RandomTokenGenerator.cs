using System.Security.Cryptography;
using System.Text;
using Maintenance.Application.Auth;

namespace Maintenance.Infrastructure.Auth;

/// <summary>
/// 32 random bytes, URL-safe encoded for the email; SHA-256 (lower-case hex) for storage.
/// The same hashing serves verification and reset tokens.
/// </summary>
public sealed class RandomTokenGenerator : ITokenGenerator
{
    private const int TokenBytes = 32;

    public GeneratedToken Generate()
    {
        var raw = Base64UrlEncode(RandomNumberGenerator.GetBytes(TokenBytes));
        return new GeneratedToken(raw, Hash(raw));
    }

    public string Hash(string raw) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw))).ToLowerInvariant();

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
