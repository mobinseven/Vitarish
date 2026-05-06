using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using Finbuckle.MultiTenant.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vitarish.Clinical.Domain.Entities;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Clinical.Infrastructure.Data;

public sealed class ClinicalDbContext : DbContext, IMultiTenantDbContext
{
    private readonly IEnumerable<IInterceptor> _interceptors;
    private readonly IRequestContext _requestContext;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ITenantInfo? TenantInfo
    {
        get
        {
            string tenantId = _requestContext.TenantId
                ?? _httpContextAccessor.HttpContext?.GetTenantId()
                ?? throw new InvalidOperationException("Tenant ID cannot be recognized.");

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

    public DbSet<ClinicalResourceLink> ResourceLinks => Set<ClinicalResourceLink>();
    public DbSet<ClinicalResourceProjection> ResourceProjections => Set<ClinicalResourceProjection>();
    public DbSet<ClinicalAuditEntry> AuditEntries => Set<ClinicalAuditEntry>();
    public DbSet<ConnectorHealthSnapshot> ConnectorHealthSnapshots => Set<ConnectorHealthSnapshot>();
    public DbSet<ClinicalFhirOperation> FhirOperations => Set<ClinicalFhirOperation>();
    public DbSet<ClinicalWorkflow> ClinicalWorkflows => Set<ClinicalWorkflow>();
    public DbSet<WorkflowStateHistory> WorkflowStateHistories => Set<WorkflowStateHistory>();
    public DbSet<ClinicalActionRecord> ClinicalActionRecords => Set<ClinicalActionRecord>();
    public DbSet<WorkflowPolicy> WorkflowPolicies => Set<WorkflowPolicy>();

    public ClinicalDbContext(
        DbContextOptions<ClinicalDbContext> options,
        IEnumerable<IInterceptor> interceptors,
        IRequestContext requestContext,
        IHttpContextAccessor httpContextAccessor)
        : base(options)
    {
        _interceptors = interceptors;
        _requestContext = requestContext;
        _httpContextAccessor = httpContextAccessor;

        this.EnforceMultiTenantOnTracking();
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) =>
        _ = optionsBuilder.AddInterceptors(_interceptors);

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

        ConfigureTenantEntity<ClinicalResourceLink>(modelBuilder);
        ConfigureTenantEntity<ClinicalResourceProjection>(modelBuilder);
        ConfigureTenantEntity<ClinicalAuditEntry>(modelBuilder);
        ConfigureTenantEntity<ConnectorHealthSnapshot>(modelBuilder);
        ConfigureTenantEntity<ClinicalFhirOperation>(modelBuilder);
        ConfigureTenantEntity<ClinicalWorkflow>(modelBuilder);
        ConfigureTenantEntity<WorkflowStateHistory>(modelBuilder);
        ConfigureTenantEntity<ClinicalActionRecord>(modelBuilder);
        ConfigureTenantEntity<WorkflowPolicy>(modelBuilder);

        _ = modelBuilder.Entity<ClinicalResourceLink>(entity =>
        {
            _ = entity.HasIndex(link => new { link.LocalEntityType, link.LocalEntityId, link.ResourceType, link.ResourceId }).IsUnique();
            _ = entity.Property(link => link.LocalEntityType).HasMaxLength(128);
            _ = entity.Property(link => link.ResourceType).HasMaxLength(128);
            _ = entity.Property(link => link.ResourceId).HasMaxLength(256);
            _ = entity.Property(link => link.ResourceVersionId).HasMaxLength(128);
            _ = entity.Property(link => link.ETag).HasMaxLength(128);
            _ = entity.Property(link => link.OwnerUserId).HasMaxLength(128);
            _ = entity.Property(link => link.PatientReference).HasMaxLength(256);
        });

        _ = modelBuilder.Entity<ClinicalResourceProjection>(entity =>
        {
            _ = entity.HasIndex(projection => new { projection.ResourceType, projection.ResourceId });
            _ = entity.Property(projection => projection.ResourceType).HasMaxLength(128);
            _ = entity.Property(projection => projection.ResourceId).HasMaxLength(256);
            _ = entity.Property(projection => projection.DisplayName).HasMaxLength(256);
            _ = entity.Property(projection => projection.PatientReference).HasMaxLength(256);
            _ = entity.Property(projection => projection.ClinicReference).HasMaxLength(256);
            _ = entity.Property(projection => projection.SourceVersionId).HasMaxLength(128);
            _ = entity.Property(projection => projection.SyncState).HasConversion<string>().HasMaxLength(32);
        });

        _ = modelBuilder.Entity<ClinicalAuditEntry>(entity =>
        {
            _ = entity.HasIndex(audit => new { audit.Category, audit.Action, audit.ResourceType, audit.ResourceId });
            _ = entity.Property(audit => audit.Category).HasMaxLength(128);
            _ = entity.Property(audit => audit.Action).HasMaxLength(128);
            _ = entity.Property(audit => audit.ActorId).HasMaxLength(128);
            _ = entity.Property(audit => audit.ResourceType).HasMaxLength(128);
            _ = entity.Property(audit => audit.ResourceId).HasMaxLength(256);
            _ = entity.Property(audit => audit.CorrelationId).HasMaxLength(128);
            _ = entity.Property(audit => audit.Outcome).HasMaxLength(64);
            _ = entity.Property(audit => audit.Reason).HasMaxLength(512);
        });

        _ = modelBuilder.Entity<ConnectorHealthSnapshot>(entity =>
        {
            _ = entity.HasIndex(snapshot => snapshot.CheckedAt);
            _ = entity.Property(snapshot => snapshot.Endpoint).HasMaxLength(256);
            _ = entity.Property(snapshot => snapshot.State).HasConversion<string>().HasMaxLength(32);
            _ = entity.Property(snapshot => snapshot.FhirVersion).HasMaxLength(64);
            _ = entity.Property(snapshot => snapshot.SoftwareName).HasMaxLength(128);
            _ = entity.Property(snapshot => snapshot.SoftwareVersion).HasMaxLength(64);
            _ = entity.Property(snapshot => snapshot.FailureMessage).HasMaxLength(1024);
            _ = entity.Property(snapshot => snapshot.SupportedResourceTypes).HasMaxLength(2048);
        });

        _ = modelBuilder.Entity<ClinicalFhirOperation>(entity =>
        {
            _ = entity.HasIndex(operation => new { operation.CorrelationId, operation.StartedAt });
            _ = entity.HasIndex(operation => new { operation.IdempotencyKey, operation.ResourceType, operation.Interaction });
            _ = entity.Property(operation => operation.ResourceType).HasMaxLength(128);
            _ = entity.Property(operation => operation.ResourceId).HasMaxLength(256);
            _ = entity.Property(operation => operation.Interaction).HasConversion<string>().HasMaxLength(32);
            _ = entity.Property(operation => operation.Status).HasConversion<string>().HasMaxLength(32);
            _ = entity.Property(operation => operation.FailureKind).HasConversion<string>().HasMaxLength(32);
            _ = entity.Property(operation => operation.CorrelationId).HasMaxLength(128);
            _ = entity.Property(operation => operation.IdempotencyKey).HasMaxLength(128);
            _ = entity.Property(operation => operation.RemoteVersionId).HasMaxLength(128);
            _ = entity.Property(operation => operation.ETag).HasMaxLength(128);
            _ = entity.Property(operation => operation.FailureMessage).HasMaxLength(1024);
        });

        _ = modelBuilder.Entity<ClinicalWorkflow>(entity =>
        {
            _ = entity.HasIndex(w => new { w.Status, w.AssignedLocationId });
            _ = entity.HasIndex(w => w.PatientFhirReference);
            _ = entity.Property(w => w.PatientFhirReference).HasMaxLength(256);
            _ = entity.Property(w => w.EncounterFhirReference).HasMaxLength(256);
            _ = entity.Property(w => w.AppointmentFhirReference).HasMaxLength(256);
            _ = entity.Property(w => w.VisitType).HasMaxLength(128);
            _ = entity.Property(w => w.AssignedProviderId).HasMaxLength(128);
            _ = entity.Property(w => w.AssignedLocationId).HasMaxLength(128);
            _ = entity.Property(w => w.Status).HasConversion<string>().HasMaxLength(32);
        });

        _ = modelBuilder.Entity<WorkflowStateHistory>(entity =>
        {
            _ = entity.HasIndex(h => new { h.WorkflowId, h.OccurredAt });
            _ = entity.Property(h => h.PreviousStatus).HasConversion<string>().HasMaxLength(32);
            _ = entity.Property(h => h.NewStatus).HasConversion<string>().HasMaxLength(32);
            _ = entity.Property(h => h.ActorId).HasMaxLength(128);
            _ = entity.Property(h => h.Reason).HasMaxLength(512);
        });

        _ = modelBuilder.Entity<ClinicalActionRecord>(entity =>
        {
            _ = entity.HasIndex(r => new { r.WorkflowId, r.ActionType, r.OccurredAt });
            _ = entity.Property(r => r.ActionType).HasConversion<string>().HasMaxLength(64);
            _ = entity.Property(r => r.ActorId).HasMaxLength(128);
            _ = entity.Property(r => r.ValidationStatus).HasMaxLength(32);
            _ = entity.Property(r => r.CommitStatus).HasMaxLength(32);
            _ = entity.Property(r => r.FhirTransactionReference).HasMaxLength(256);
        });

        _ = modelBuilder.Entity<WorkflowPolicy>(entity =>
        {
            _ = entity.HasIndex(p => new { p.VisitType, p.IsActive });
            _ = entity.Property(p => p.VisitType).HasMaxLength(128);
        });

        if (Database.IsSqlite())
        {
            _ = modelBuilder.ConfigureEntityDateTimeOffsetConversions();
        }
    }

    private static void ConfigureTenantEntity<TEntity>(ModelBuilder modelBuilder)
        where TEntity : Entity
    {
        EntityTypeBuilder<TEntity> entityBuilder = modelBuilder.Entity<TEntity>();

        _ = entityBuilder.HasQueryFilter(entity => EF.Property<DateTimeOffset?>(entity, nameof(Entity.DeletedAt)) == null);
        _ = entityBuilder.IsMultiTenant();
    }
}
