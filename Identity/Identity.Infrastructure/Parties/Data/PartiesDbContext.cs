using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vitarish.Identity.Core.Parties.Domain.Entities;
using Vitarish.Identity.Core.Parties.Domain.Events;
using Vitarish.Identity.Infrastructure.Parties.Extensions;

namespace Vitarish.Identity.Infrastructure.Parties.Data;

/// <summary>
/// EF Core database context for parties management.
/// </summary>
public class PartiesDbContext(DbContextOptions<PartiesDbContext> options, MediatR.IPublisher publisher) : DbContext(options)
{
    public DbSet<Owner> Owners => Set<Owner>();

    public DbSet<Tenant> Tenants => Set<Tenant>();

    public DbSet<TenantIndicator> TenantIndicators => Set<TenantIndicator>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        _ = modelBuilder.Entity<Tenant>(entity =>
        {
            _ = entity.HasIndex(t => t.ClientId);

            _ = entity.HasOne(t => t.Owner)
                .WithMany()
                .HasForeignKey(t => t.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        _ = modelBuilder.Entity<TenantIndicator>(entity =>
        {
            _ = entity.HasKey(i => i.Id);
            _ = entity.Property(i => i.Value).HasMaxLength(253).IsRequired();
            _ = entity.HasIndex(i => new { i.Method, i.Value }).IsUnique();

            _ = entity.HasIndex(i => new { i.TenantId, i.IsPrimary })
                .HasFilter("[IsPrimary] = CAST(1 AS bit)")
                .IsUnique();

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

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (EntityEntry<TenantIndicator> entry in ChangeTracker.Entries<TenantIndicator>()
                     .Where(e => e.State is EntityState.Added or EntityState.Modified))
        {
            try
            {
                entry.Entity.UpdateLastChecked();
                entry.Property(i => i.Value).CurrentValue = HostnameNormalizer.Canonical(entry.Entity.Value);
            }
            catch (ArgumentException ex)
            {
                throw new DbUpdateException(
                    $"Invalid hostname value '{entry.Entity.Value}' for TenantIndicator (Id: {entry.Entity.Id}).",
                    ex);
            }
        }

        List<TenantIndicatorDeletedEvent> deletedIndicators = ChangeTracker.Entries<TenantIndicator>()
            .Where(e => e.State == EntityState.Deleted)
            .Select(e => new TenantIndicatorDeletedEvent(
                e.Entity.TenantId,
                e.Entity.Id,
                e.Entity.Method,
                e.Entity.Value))
            .ToList();

        int result = await base.SaveChangesAsync(cancellationToken);

        foreach (TenantIndicatorDeletedEvent ev in deletedIndicators)
        {
            await publisher.Publish(ev, cancellationToken);
        }

        return result;
    }
}
