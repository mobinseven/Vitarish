
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vitarish.Parties.Domain.Entities;
using Vitarish.Parties.Domain.Events;
using Vitarish.Parties.Infrastructure.Extensions;

namespace Vitarish.Parties.Infrastructure.Data;
/// <summary>
/// EF Core database context for parties management.
/// </summary>
public class PartiesDbContext(DbContextOptions<PartiesDbContext> options, MediatR.IPublisher publisher) : DbContext(options)
{
    /// <summary>Owners set.</summary>
    public DbSet<Owner> Owners => Set<Owner>();

    /// <summary>Tenants set.</summary>
    public DbSet<Tenant> Tenants => Set<Tenant>();

    /// <summary>Tenant indicators set.</summary>
    public DbSet<TenantIndicator> TenantIndicators => Set<TenantIndicator>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        _ = modelBuilder.Entity<Tenant>(entity =>
        {
            _ = entity.HasIndex(t => t.ClientId);
        });

        _ = modelBuilder.Entity<TenantIndicator>(entity =>
        {
            _ = entity.HasKey(i => i.Id);
            _ = entity.Property(i => i.Value).HasMaxLength(253).IsRequired();
            _ = entity.HasIndex(i => new { i.Method, i.Value }).IsUnique();

            IndexBuilder<TenantIndicator> primaryIdx = entity.HasIndex(i => new { i.TenantId, i.IsPrimary }).IsUnique();
            primaryIdx.Metadata.SetAnnotation(RelationalAnnotationNames.Filter, "[IsPrimary] = CAST(1 AS bit)");

            _ = entity.HasOne(i => i.Tenant)
                .WithMany(t => t.Indicators)
                .HasForeignKey(i => i.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            _ = entity.OwnsOne(i => i.Verification, v =>
            {
                _ = v.Property(x => x.Evidence).HasMaxLength(253).IsRequired();
                _ = v.Property(x => x.EvidenceExpectedHash).HasMaxLength(128).IsRequired();
                _ = v.Property(x => x.Token).HasMaxLength(64);
            });
        });
    }

    /// <inheritdoc />
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Normalize hostnames
        foreach (EntityEntry<TenantIndicator>? entry in ChangeTracker.Entries<TenantIndicator>()
            .Where(e => e.State is EntityState.Added or EntityState.Modified))
        {
            try
            {
                entry.Entity.Value = HostnameNormalizer.Canonical(entry.Entity.Value);
            }
            catch (ArgumentException ex)
            {
                throw new DbUpdateException(
                    $"Invalid hostname value '{entry.Entity.Value}' for TenantIndicator (Id: {entry.Entity.Id}).",
                    ex
                );
            }
        }

        // Collect deleted indicators for event publishing
        List<TenantIndicatorDeletedEvent> deletedIndicators = ChangeTracker.Entries<TenantIndicator>()
            .Where(e => e.State == EntityState.Deleted)
            .Select(e => new Domain.Events.TenantIndicatorDeletedEvent(
                e.Entity.TenantId,
                e.Entity.Id,
                e.Entity.Method,
                e.Entity.Value))
            .ToList();

        int result = await base.SaveChangesAsync(cancellationToken);

        // Publish delete events after successful save
        foreach (TenantIndicatorDeletedEvent? ev in deletedIndicators)
        {
            await publisher.Publish(ev, cancellationToken);
        }

        return result;
    }
}
