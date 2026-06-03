using Finbuckle.MultiTenant.EntityFrameworkCore;

namespace Vitarish.Entities.Server.Data;

/// <summary>
/// Creates transient multi-tenant EF Core contexts with the platform context creation path.
/// </summary>
public sealed class TransientMultiTenantDbContextFactory<TDbContext>(
    IServiceProvider serviceProvider,
    DbContextOptions<TDbContext> options)
    : DbContextFactoryBase<TDbContext>(serviceProvider, options)
    where TDbContext : DbContext, IMultiTenantDbContext;
