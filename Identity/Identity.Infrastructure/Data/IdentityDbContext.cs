using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vitarish.Identity.Core.Domain.Entities;
using Vitarish.Identity.Core.Domain.Events;
using Vitarish.Identity.Core.Extensions;

namespace Vitarish.Identity.Infrastructure.Data;

/// <summary>
/// EF Core database context for identity management (parties/tenants).
/// </summary>
public class IdentityDbContext(DbContextOptions<IdentityDbContext> options, IPublisher publisher) : DbContext(options)
{
    /// <summary>Owners set.</summary>
    public DbSet<Owner> Owners => Set<Owner>();

    /// <summary>Tenants set.</summary>
    public DbSet<Tenant> Tenants => Set<Tenant>();

    /// <summary>Tenant indicators set.</summary>
    public DbSet<TenantIndicator> TenantIndicators => Set<TenantIndicator>();

    /// <summary>Authentication clients set.</summary>
    public DbSet<AuthenticationClient> AuthenticationClients => Set<AuthenticationClient>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        _ = modelBuilder.Entity<Owner>(entity =>
        {
            _ = entity.HasKey(o => o.Id);
            _ = entity.Property(o => o.Name).HasMaxLength(256).IsRequired();
            _ = entity.Property(o => o.LogoUrl).HasMaxLength(2048);
            _ = entity.Property(o => o.Description).HasMaxLength(1024);
            _ = entity.Property(o => o.CreatedBy).HasMaxLength(128);
            _ = entity.Property(o => o.ModifiedBy).HasMaxLength(128);
        });

        _ = modelBuilder.Entity<Tenant>(entity =>
        {
            _ = entity.HasKey(t => t.Id);
            _ = entity.Property(t => t.Name).HasMaxLength(256).IsRequired();
            _ = entity.Property(t => t.ClientId).HasMaxLength(128).IsRequired();
            _ = entity.Property(t => t.LogoUrl).HasMaxLength(2048);
            _ = entity.Property(t => t.Description).HasMaxLength(1024);
            _ = entity.Property(t => t.CreatedBy).HasMaxLength(128);
            _ = entity.Property(t => t.ModifiedBy).HasMaxLength(128);
            _ = entity.HasIndex(t => t.ClientId);
        });

        _ = modelBuilder.Entity<TenantIndicator>(entity =>
        {
            _ = entity.HasKey(i => i.Id);
            _ = entity.Property(i => i.Value).HasMaxLength(253).IsRequired();
            _ = entity.HasIndex(i => new { i.Method, i.Value }).IsUnique();

            // Create a filtered unique index for primary indicators.
            // Note: This filter syntax is provider-specific. The SQL Server syntax is used here.
            // For other providers (PostgreSQL, SQLite), this may need to be adjusted:
            // - PostgreSQL: "[IsPrimary] = true" or using HasFilter with EF's built-in methods
            // - SQLite: "IsPrimary = 1"
            // Consider using conditional compilation or provider detection if multi-database support is needed.
            IndexBuilder<TenantIndicator> primaryIdx = entity.HasIndex(i => new { i.TenantId, i.IsPrimary }).IsUnique();
            primaryIdx.HasFilter("[IsPrimary] = 1");

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

        _ = modelBuilder.Entity<AuthenticationClient>(entity =>
        {
            _ = entity.HasKey(c => c.Id);
            _ = entity.Property(c => c.Id).HasMaxLength(64);
            _ = entity.Property(c => c.Url).HasMaxLength(2048).IsRequired();
            _ = entity.Property(c => c.Title).HasMaxLength(256);
            _ = entity.Property(c => c.Description).HasMaxLength(1024);
            _ = entity.Property(c => c.LogoUrl).HasMaxLength(2048);
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
            .Select(e => new TenantIndicatorDeletedEvent(
                e.Entity.TenantId,
                e.Entity.Id,
                e.Entity.Method,
                e.Entity.Value))
            .ToList();

        var result = await base.SaveChangesAsync(cancellationToken);

        // Publish delete events after successful save
        foreach (TenantIndicatorDeletedEvent? ev in deletedIndicators)
        {
            await publisher.Publish(ev, cancellationToken);
        }

        return result;
    }
}
