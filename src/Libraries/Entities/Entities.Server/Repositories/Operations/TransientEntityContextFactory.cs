namespace Vitarish.Entities.Server.Repositories.Operations;

/// <summary>
/// Creates fresh <typeparamref name="TDbContext"/> instances per operation using EF Core's
/// <see cref="IDbContextFactory{TContext}"/>.
/// Each call to <see cref="CreateScope"/> returns a new, independent context that is disposed
/// at the end of the operation, providing complete context isolation between repository calls.
/// </summary>
internal sealed class TransientEntityContextFactory<TDbContext>(IDbContextFactory<TDbContext> dbContextFactory)
    : IEntityContextFactory<TDbContext>
    where TDbContext : DbContext
{
    public IEntityContextScope<TDbContext> CreateScope()
    {
        TDbContext context = dbContextFactory.CreateDbContext();
        return new TransientEntityContextScope<TDbContext>(context);
    }
}
