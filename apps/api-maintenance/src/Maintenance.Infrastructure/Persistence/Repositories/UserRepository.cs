using Maintenance.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Maintenance.Infrastructure.Persistence.Repositories;

public sealed class UserRepository(MaintenanceDbContext context) : IUserRepository
{
    public Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var normalized = User.NormalizeEmail(email);
        return context.Users.SingleOrDefaultAsync(user => user.Email == normalized, cancellationToken);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken) =>
        await context.Users.AddAsync(user, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => context.SaveChangesAsync(cancellationToken);
}
