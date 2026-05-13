namespace Vitarish.Entities.Server.Repositories.UnitOfWork;

/// <summary>
/// Creates units of work for coordinating multiple database operations within a single
/// transaction. Register via
/// <see cref="ServiceCollectionExtensions.AddEntityUnitOfWorkFactory{TDbContext}"/>.
/// </summary>
public interface IEntityUnitOfWorkFactory<TDbContext>
    where TDbContext : DbContext
{
    /// <summary>
    /// Begins a new unit of work, opening a database transaction on a fresh context.
    /// </summary>
    Task<IEntityUnitOfWork<TDbContext>> BeginAsync(CancellationToken cancellationToken = default);
}
