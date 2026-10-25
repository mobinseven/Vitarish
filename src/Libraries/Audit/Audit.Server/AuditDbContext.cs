using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using Finbuckle.MultiTenant.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Vitarish.Audit.Domain;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Entities.Server.Infrastructure;

namespace Vitarish.Audit.Server;

public sealed class AuditDbContext : DbContext, IMultiTenantDbContext
{
    private readonly IRequestContext _requestContext;
    private readonly IMultiTenantContextAccessor<TenantInfo>? _multiTenantContextAccessor;
    private string? _rowScopeTenantId;

    public AuditDbContext(
        DbContextOptions<AuditDbContext> options,
        IRequestContext requestContext,
        IMultiTenantContextAccessor<TenantInfo>? multiTenantContextAccessor = null)
        : base(options)
    {
        _requestContext = requestContext;
        _multiTenantContextAccessor = multiTenantContextAccessor;
        this.EnforceMultiTenantOnTracking();
    }

    public ITenantInfo? TenantInfo
    {
        get
        {
            string? tenantId = _rowScopeTenantId
                ?? _multiTenantContextAccessor?.MultiTenantContext?.TenantInfo?.Id
                ?? _requestContext.TenantId;
            if (string.IsNullOrWhiteSpace(tenantId))
                throw new InvalidOperationException("Tenant ID cannot be recognized.");
            return new TenantInfo
            {
                Id = tenantId,
                Identifier = tenantId,
                Name = tenantId
            };
        }
    }

    /// <summary>
    /// Scopes this context to the tenant that owns the audit rows being written,
    /// which can differ from the request's tenant: a clinic created from the
    /// onboarding host, or a platform-scoped event from a request with no tenant.
    /// Only audit writers call this, with the scope of the rows they write.
    /// </summary>
    internal void UseRowScope(string tenantId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        _rowScopeTenantId = tenantId;
    }

    public TenantMismatchMode TenantMismatchMode { get; set; } = TenantMismatchMode.Throw;
    public TenantNotSetMode TenantNotSetMode { get; set; } = TenantNotSetMode.Throw;

    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();
    public DbSet<SecurityAuditEvent> SecurityAuditEvents => Set<SecurityAuditEvent>();

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
        _ = modelBuilder.ConfigureUtcDateTimeColumns();
        _ = modelBuilder.HasDefaultSchema(nameof(Audit));

        _ = modelBuilder.ConfigureMultiTenant();

        _ = modelBuilder.Entity<AuditEntry>(entity =>
        {
            _ = entity.HasKey(e => e.Id);
            _ = entity.Property(e => e.Action).HasMaxLength(256);
            _ = entity.Property(e => e.ActorId).HasMaxLength(256);
            _ = entity.Property(e => e.EntityType).HasMaxLength(256);
            _ = entity.Property(e => e.TenantId).HasMaxLength(64);
            _ = entity.Property(e => e.SnapshotBefore);
            _ = entity.Property(e => e.SnapshotAfter);
            _ = entity.Property(e => e.OccurredAt);
            _ = entity.IsMultiTenant();
            _ = entity.HasIndex(e => new { e.EntityType, e.EntityId, e.OccurredAt });
            _ = entity.HasIndex(e => new { e.ActorId, e.OccurredAt });
            _ = entity.HasIndex(e => e.OccurredAt);
        });

        _ = modelBuilder.Entity<SecurityAuditEvent>(entity =>
        {
            _ = entity.HasKey(e => e.Id);
            _ = entity.Property(e => e.EventName).HasMaxLength(256);
            _ = entity.Property(e => e.EventCategory).HasMaxLength(64);
            _ = entity.Property(e => e.Outcome).HasMaxLength(32);
            _ = entity.Property(e => e.ActorKind).HasMaxLength(32);
            _ = entity.Property(e => e.ActorId).HasMaxLength(256);
            _ = entity.Property(e => e.SubjectId).HasMaxLength(256);
            _ = entity.Property(e => e.TenantId).HasMaxLength(64);
            _ = entity.Property(e => e.Resource).HasMaxLength(512);
            _ = entity.Property(e => e.Operation).HasMaxLength(256);
            _ = entity.Property(e => e.CorrelationId).HasMaxLength(256);
            _ = entity.Property(e => e.SourceBoundary).HasMaxLength(256);
            _ = entity.Property(e => e.SourceIpKind).HasMaxLength(32);
            _ = entity.Property(e => e.SourceIpAddress).HasMaxLength(64);
            _ = entity.Property(e => e.ClientInterfaceHost).HasMaxLength(253);
            _ = entity.Property(e => e.RedactedReason).HasMaxLength(256);
            _ = entity.Property(e => e.Changes)
                .HasConversion(SecurityAuditChangeColumn.Converter, SecurityAuditChangeColumn.Comparer)
                .HasMaxLength(SecurityAuditChangeColumn.MaximumLength);
            _ = entity.Property(e => e.IdempotencyKey).HasMaxLength(256);
            _ = entity.Property(e => e.OccurredAt);
            _ = entity.IsMultiTenant();
            _ = entity.HasIndex(e => new { e.TenantId, e.IdempotencyKey }).IsUnique();
            _ = entity.HasIndex(e => new { e.Resource, e.OccurredAt });
            _ = entity.HasIndex(e => new { e.TenantId, e.SubjectId, e.OccurredAt });
            _ = entity.HasIndex(e => e.OccurredAt);
        });
    }
}
