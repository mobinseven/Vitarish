using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using Finbuckle.MultiTenant.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Vitarish.Audit.Domain;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Audit.Server;

public sealed class AuditDbContext : DbContext, IMultiTenantDbContext
{
    private readonly IRequestContext _requestContext;

    public AuditDbContext(DbContextOptions<AuditDbContext> options, IRequestContext requestContext)
        : base(options)
    {
        _requestContext = requestContext;
        this.EnforceMultiTenantOnTracking();
    }

    public ITenantInfo? TenantInfo
    {
        get
        {
            string tenantId = _requestContext.TenantId ?? throw new InvalidOperationException("Tenant ID can not be recognized.");
            return new TenantInfo
            {
                Id = tenantId,
                Identifier = tenantId,
                Name = tenantId
            };
        }
    }

    public TenantMismatchMode TenantMismatchMode { get; set; } = TenantMismatchMode.Throw;
    public TenantNotSetMode TenantNotSetMode { get; set; } = TenantNotSetMode.Throw;

    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        this.EnforceMultiTenant();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        this.EnforceMultiTenant();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        _ = modelBuilder.ConfigureMultiTenant();

        _ = modelBuilder.Entity<AuditEntry>(entity =>
        {
            _ = entity.HasKey(e => e.Id);
            _ = entity.Property(e => e.Action).HasMaxLength(256);
            _ = entity.Property(e => e.ActorId).HasMaxLength(256);
            _ = entity.Property(e => e.EntityType).HasMaxLength(256);
            _ = entity.Property(e => e.TenantId).HasMaxLength(64);
            _ = entity.Property(e => e.SnapshotBefore).HasColumnType("nvarchar(max)");
            _ = entity.Property(e => e.SnapshotAfter).HasColumnType("nvarchar(max)");
            _ = entity.Property(e => e.OccurredAt);
            _ = entity.IsMultiTenant();
            _ = entity.HasIndex(e => new { e.EntityType, e.EntityId, e.OccurredAt });
            _ = entity.HasIndex(e => new { e.ActorId, e.OccurredAt });
            _ = entity.HasIndex(e => e.OccurredAt);
        });
    }
}
