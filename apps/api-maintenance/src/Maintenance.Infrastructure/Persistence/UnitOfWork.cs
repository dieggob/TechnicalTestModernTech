using Maintenance.Application.Persistence;
using Microsoft.EntityFrameworkCore.Storage;

namespace Maintenance.Infrastructure.Persistence;

/// <summary>The DbContext is the unit of work; this adapter exposes it through the Application interface.</summary>
public sealed class UnitOfWork(MaintenanceDbContext context) : IUnitOfWork
{
    public async Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken cancellationToken) =>
        new Transaction(await context.Database.BeginTransactionAsync(cancellationToken));

    public Task SaveChangesAsync(CancellationToken cancellationToken) => context.SaveChangesAsync(cancellationToken);

    private sealed class Transaction(IDbContextTransaction inner) : IUnitOfWorkTransaction
    {
        public Task CommitAsync(CancellationToken cancellationToken) => inner.CommitAsync(cancellationToken);

        /// <summary>EF rolls back an uncommitted transaction on dispose.</summary>
        public ValueTask DisposeAsync() => inner.DisposeAsync();
    }
}
