namespace Maintenance.Domain.Users;

public interface IUserRepository
{
    /// <summary>Looks up by normalised email; callers pass any casing.</summary>
    Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken);

    Task AddAsync(User user, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
