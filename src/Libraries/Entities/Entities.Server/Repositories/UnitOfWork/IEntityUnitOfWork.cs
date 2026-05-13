namespace Vitarish.Entities.Server.Repositories.UnitOfWork;

/// <summary>
/// Represents a unit of work that coordinates multiple database operations within a single
/// transaction. Obtain an instance via <see cref="IEntityUnitOfWorkFactory{TDbContext}"/>.
/// </summary>
/// <remarks>
/// Dispose the unit of work after use — <see cref="IAsyncDisposable.DisposeAsync"/> rolls
/// back any uncommitted transaction and releases the underlying context.
/// </remarks>
public interface IEntityUnitOfWork<TDbContext> : IAsyncDisposable
    where TDbContext : DbContext
{
    /// <summary>The database context shared across all operations within this unit of work.</summary>
    TDbContext Context { get; }

    /// <summary>Commits all changes made within this unit of work to the database.</summary>
    Task CommitAsync(CancellationToken cancellationToken = default);

    /// <summary>Rolls back all changes made within this unit of work.</summary>
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
