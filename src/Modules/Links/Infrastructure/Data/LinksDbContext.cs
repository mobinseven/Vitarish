using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Vitarish.Links.Infrastructure.Data;

/// <summary>
/// DbContext for the Links module.
/// Links are NOT multi-tenant at the database level because resolution is global:
/// a (Domain, Slug) pair uniquely identifies a link across all tenants.
/// The TenantId column records which tenant owns the link.
/// </summary>
public sealed class LinksDbContext(
    DbContextOptions<LinksDbContext> options,
    IEnumerable<IInterceptor> interceptors) : DbContext(options)
{
    private readonly IEnumerable<IInterceptor> _interceptors = interceptors;

    public DbSet<Link> Links => Set<Link>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => _ = optionsBuilder.AddInterceptors(_interceptors);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        _ = modelBuilder.HasDefaultSchema(nameof(Links));

        _ = modelBuilder.Entity<Link>(entity =>
        {
            _ = entity.HasQueryFilter(e => e.DeletedAt == null);

            _ = entity.Property(e => e.Domain).HasMaxLength(253);
            _ = entity.Property(e => e.Slug).HasMaxLength(32).UseCollation(LinkRules.SlugCollation);
            _ = entity.Property(e => e.DestinationUrl).HasMaxLength(2048);
            _ = entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(32);

            _ = entity.HasIndex(e => new { e.Domain, e.Slug }).IsUnique();
            _ = entity.HasIndex(e => e.TenantId);
        });
    }
}
