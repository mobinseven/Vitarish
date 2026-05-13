namespace Vitarish.Entities.Server.Repositories.Operations;

/// <summary>
/// Represents a scoped handle to a <typeparamref name="TDbContext"/> for a single repository operation.
/// Disposing the scope releases the context when it is owned by this scope.
/// </summary>
internal interface IEntityContextScope<TDbContext> : IAsyncDisposable
    where TDbContext : DbContext
{
    TDbContext Context { get; }
}
