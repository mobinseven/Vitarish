namespace Vitarish.Entities.Server.Data;

/// <summary>
/// Creates transient non-tenant EF Core contexts with the platform context creation path.
/// </summary>
public sealed class TransientDbContextFactory<TDbContext>(
    IServiceProvider serviceProvider,
    DbContextOptions<TDbContext> options)
    : DbContextFactoryBase<TDbContext>(serviceProvider, options)
    where TDbContext : DbContext;
