namespace Maintenance.Application.Persistence;

/// <summary>
/// Groups changes across repositories into one commit. Services that touch two aggregates in
/// one behaviour (a maintenance record and its vehicle's mileage) open a transaction so the two
/// can never disagree after a partial failure (design decision).
/// </summary>
public interface IUnitOfWork
{
    Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

/// <summary>Disposing without <see cref="CommitAsync"/> rolls the transaction back.</summary>
public interface IUnitOfWorkTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken);
}
