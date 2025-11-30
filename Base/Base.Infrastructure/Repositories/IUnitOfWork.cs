namespace Vitarish.Base.Infrastructure.Repositories;

/// <summary>
/// Unit of work interface for managing database transactions.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Saves all changes made in this unit of work.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
