using Maintenance.Application.Auth;
using Maintenance.Domain.Users;
using Microsoft.AspNetCore.Identity;

namespace Maintenance.Infrastructure.Auth;

/// <summary>
/// Adapter over ASP.NET Core Identity's PasswordHasher (PBKDF2-HMAC-SHA512, salted, 100k iterations).
/// Keeps Identity types out of Application; swapping the algorithm is a change to this class only.
/// </summary>
public sealed class IdentityPasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<User> _hasher = new();

    public string Hash(string password) => _hasher.HashPassword(null!, password);

    public bool Verify(string password, string passwordHash) =>
        _hasher.VerifyHashedPassword(null!, passwordHash, password) is not PasswordVerificationResult.Failed;
}
