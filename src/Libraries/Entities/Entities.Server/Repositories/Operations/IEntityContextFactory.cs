namespace Vitarish.Entities.Server.Repositories.Operations;

/// <summary>
/// Creates per-operation context scopes for use within repository methods.
/// Register a concrete implementation via
/// <see cref="ServiceCollectionExtensions.AddEntityContextFactory{TDbContext}"/> to enable
/// per-operation context isolation. When no factory is registered, repositories fall back to
/// the ambient (constructor-injected) context, preserving existing behavior.
/// </summary>
internal interface IEntityContextFactory<TDbContext>
    where TDbContext : DbContext
{
    IEntityContextScope<TDbContext> CreateScope();
}
