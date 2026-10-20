using Vitarish.Entities.Server.Infrastructure;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Text.Json;
using Vitarish.Parties.Domain.Entities;
using Vitarish.Parties.Domain.Events;
using Vitarish.Parties.Domain.Models;
using Vitarish.Parties.Domain.Profiles;
using Vitarish.Parties.Infrastructure.Extensions;

namespace Vitarish.Parties.Infrastructure.Data;
/// <summary>
/// EF Core database context for parties management.
/// </summary>
public class PartiesDbContext(DbContextOptions<PartiesDbContext> options, MediatR.IPublisher publisher, IEnumerable<IInterceptor> interceptors) : DbContext(options)
{
    /// <summary>Owners set.</summary>
    public DbSet<Owner> Owners => Set<Owner>();

    /// <summary>Tenants set.</summary>
    public DbSet<Tenant> Tenants => Set<Tenant>();

    /// <summary>Tenant indicators set.</summary>
    public DbSet<TenantIndicator> TenantIndicators => Set<TenantIndicator>();

    /// <summary>Tenants' business categories and public listing details.</summary>
    public DbSet<TenantBusinessProfile> TenantBusinessProfiles => Set<TenantBusinessProfile>();

    /// <summary>User tenants view (cross-schema view based on current user).</summary>
    public DbSet<UserTenant> UserTenants => Set<UserTenant>();
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) => _ = optionsBuilder
            .AddInterceptors(interceptors);

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        _ = modelBuilder.ConfigureUtcDateTimeColumns();
        _ = modelBuilder.HasDefaultSchema(nameof(Parties));

        _ = modelBuilder.Entity<UserTenant>(eb =>
        {
            _ = eb.ToView("vw_UserTenant");
            _ = eb.HasNoKey();
        });

        _ = modelBuilder.Entity<Tenant>(entity =>
        {
            _ = entity.HasIndex(t => t.AppId);

            _ = entity.Property(e => e.Metadata)
                        .HasConversion(
                            v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                            v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions)null))
                        .HasDefaultValueSql("'{}'");
        });

        _ = modelBuilder.Entity<TenantBusinessProfile>(entity =>
        {
            _ = entity.HasKey(profile => profile.TenantId);
            _ = entity.HasOne<Tenant>().WithOne().HasForeignKey<TenantBusinessProfile>(profile => profile.TenantId);
            _ = entity.Property(profile => profile.Category).HasMaxLength(TenantBusinessProfile.CategoryMaximumLength);
            _ = entity.Property(profile => profile.Details)
                .HasColumnType("jsonb")
                .HasConversion(
                    details => details == null ? null : TenantBusinessDetailsDocument.Serialize(details),
                    json => json == null ? null : TenantBusinessDetailsDocument.Deserialize(json),
                    new ValueComparer<TenantBusinessDetails?>(
                        (left, right) => left == null ? right == null : left.IsEquivalentTo(right),
                        details => details == null ? 0 : TenantBusinessDetailsDocument.Serialize(details).GetHashCode(),
                        details => details));
        });

        _ = modelBuilder.Entity<TenantIndicator>(entity =>
        {
            _ = entity.HasKey(i => i.Id);
            _ = entity.Property(i => i.Value).HasMaxLength(253).IsRequired();
            _ = entity.HasIndex(i => new { i.Method, i.Value }).IsUnique();

            _ = entity.HasIndex(i => new { i.TenantId, i.IsPrimary })
                .IsUnique()
                .HasFilter("\"IsPrimary\" = TRUE");

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
        List<TenantIndicatorDeletedEvent> deletedIndicators = [.. ChangeTracker.Entries<TenantIndicator>()
            .Where(e => e.State == EntityState.Deleted)
            .Select(e => new Domain.Events.TenantIndicatorDeletedEvent(
                e.Entity.TenantId,
                e.Entity.Id,
                e.Entity.Method,
                e.Entity.Value))];

        HashSet<string> changedHostnames = new(StringComparer.Ordinal);
        foreach (EntityEntry<TenantIndicator> entry in ChangeTracker.Entries<TenantIndicator>())
        {
            if (entry.State is EntityState.Added or EntityState.Modified)
                changedHostnames.Add(entry.Entity.Value);
            if (entry.State is EntityState.Modified or EntityState.Deleted)
                changedHostnames.Add(entry.Property(indicator => indicator.Value).OriginalValue);
        }

        int result = await base.SaveChangesAsync(cancellationToken);

        // Publish delete events after successful save
        foreach (TenantIndicatorDeletedEvent? ev in deletedIndicators)
        {
            await publisher.Publish(ev, cancellationToken);
        }

        if (changedHostnames.Count > 0)
            await publisher.Publish(new Domain.Events.TenantHostnamesChangedEvent([.. changedHostnames]), cancellationToken);

        return result;
    }
}
