using Microsoft.EntityFrameworkCore.Storage;

namespace Vitarish.Entities.Server.Repositories.UnitOfWork;

internal sealed class EntityUnitOfWork<TDbContext>(TDbContext context, IDbContextTransaction transaction)
    : IEntityUnitOfWork<TDbContext>
    where TDbContext : DbContext
{
    private int _disposed;

    public TDbContext Context { get; } = context;

    public Task CommitAsync(CancellationToken cancellationToken = default)
        => transaction.CommitAsync(cancellationToken);

    public Task RollbackAsync(CancellationToken cancellationToken = default)
        => transaction.RollbackAsync(cancellationToken);

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0)
        {
            await transaction.DisposeAsync();
            await Context.DisposeAsync();
        }
    }
}
