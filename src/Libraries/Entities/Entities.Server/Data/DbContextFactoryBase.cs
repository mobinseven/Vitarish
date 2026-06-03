using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Vitarish.Entities.Server.Data;

/// <summary>
/// Creates transient EF Core contexts through a cached activator while reusing the configured options instance.
/// </summary>
public abstract class DbContextFactoryBase<TDbContext>(
    IServiceProvider serviceProvider,
    DbContextOptions<TDbContext> options) : IDbContextFactory<TDbContext>
    where TDbContext : DbContext
{
    private static readonly ObjectFactory ContextFactory = ActivatorUtilities.CreateFactory(
        typeof(TDbContext),
        [typeof(DbContextOptions<TDbContext>)]);

    public TDbContext CreateDbContext()
    {
        ValidateCreationRequest();
        return (TDbContext)ContextFactory(serviceProvider, [options]);
    }

    protected virtual void ValidateCreationRequest()
    {
    }
}
