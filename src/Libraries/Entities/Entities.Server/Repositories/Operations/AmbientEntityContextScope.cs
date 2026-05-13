namespace Vitarish.Entities.Server.Repositories.Operations;

/// <summary>
/// An operation scope that wraps an externally-managed <typeparamref name="TDbContext"/>.
/// This scope does not own the context and will not dispose it.
/// Used as the default backward-compatible scope when a repository retains a constructor-injected context.
/// </summary>
internal sealed class AmbientEntityContextScope<TDbContext>(TDbContext context) : IEntityContextScope<TDbContext>
    where TDbContext : DbContext
{
    public TDbContext Context { get; } = context;

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
