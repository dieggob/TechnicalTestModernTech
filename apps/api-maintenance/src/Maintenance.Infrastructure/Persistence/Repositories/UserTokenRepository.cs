using Maintenance.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Maintenance.Infrastructure.Persistence.Repositories;

public sealed class UserTokenRepository(MaintenanceDbContext context) : IUserTokenRepository
{
    public Task<UserToken?> FindByHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        context.UserTokens.SingleOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

    public async Task AddAsync(UserToken token, CancellationToken cancellationToken) =>
        await context.UserTokens.AddAsync(token, cancellationToken);

    public Task<int> DeleteCreatedBeforeAsync(DateTime cutoff, CancellationToken cancellationToken) =>
        context.UserTokens.Where(token => token.CreatedAt < cutoff).ExecuteDeleteAsync(cancellationToken);

    public async Task SupersedeAsync(Guid userId, TokenPurpose purpose, DateTime now, CancellationToken cancellationToken)
    {
        var usable = await context.UserTokens
            .Where(token => token.UserId == userId && token.Purpose == purpose && token.UsedAt == null && token.ExpiresAt > now)
            .ToListAsync(cancellationToken);

        foreach (var token in usable)
        {
            token.MarkUsed(now);
        }
    }
}
