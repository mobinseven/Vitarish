namespace Vitarish.Entities.Server.Repositories.Operations;

/// <summary>
/// An operation scope that owns a freshly-created <typeparamref name="TDbContext"/>.
/// Disposing this scope disposes the underlying context.
/// Used when per-operation context isolation is required.
/// </summary>
internal sealed class TransientEntityContextScope<TDbContext>(TDbContext context) : IEntityContextScope<TDbContext>
    where TDbContext : DbContext
{
    private int _disposed;

    public TDbContext Context { get; } = context;

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0)
        {
            await Context.DisposeAsync();
        }
    }
}
