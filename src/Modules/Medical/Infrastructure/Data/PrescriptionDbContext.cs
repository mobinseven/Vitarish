using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using Finbuckle.MultiTenant.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vitarish.Entities.Server.Infrastructure;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Medical.Infrastructure.Data;

public sealed class PrescriptionDbContext : DbContext, IMultiTenantDbContext
{
    private readonly IEnumerable<IInterceptor> _interceptors;
    private readonly IRequestContext _requestContext;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public PrescriptionDbContext(
        DbContextOptions<PrescriptionDbContext> options,
        IRequestContext requestContext,
        IHttpContextAccessor httpContextAccessor,
        IEnumerable<IInterceptor> interceptors) : base(options)
    {
        _requestContext = requestContext;
        _httpContextAccessor = httpContextAccessor;
        _interceptors = interceptors;

        this.EnforceMultiTenantOnTracking();
    }

    public ITenantInfo? TenantInfo
    {
        get
        {
            string tenantId = _requestContext.TenantId
                ?? _httpContextAccessor.HttpContext?.GetTenantId()
                ?? throw new InvalidOperationException("Tenant ID cannot be recognized for prescription persistence.");

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

    public DbSet<DoctorProviderCredential> DoctorProviderCredentials => Set<DoctorProviderCredential>();
    public DbSet<PrescriptionRecord> Prescriptions => Set<PrescriptionRecord>();
    public DbSet<PrescriptionItem> PrescriptionItems => Set<PrescriptionItem>();
    public DbSet<PrescriptionTemplate> PrescriptionTemplates => Set<PrescriptionTemplate>();
    public DbSet<PrescriptionTemplateItem> PrescriptionTemplateItems => Set<PrescriptionTemplateItem>();
    public DbSet<PrescriptionProviderSubmission> PrescriptionSubmissions => Set<PrescriptionProviderSubmission>();
    public DbSet<PrescriptionOutboxRecord> OutboxRecords => Set<PrescriptionOutboxRecord>();
    public DbSet<PrescriptionEscalation> Escalations => Set<PrescriptionEscalation>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) => _ = optionsBuilder.AddInterceptors(_interceptors);

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
        _ = modelBuilder.HasDefaultSchema("Prescription");
        _ = modelBuilder.ConfigureMultiTenant();

        ConfigureDoctorProviderCredential(modelBuilder);
        ConfigurePrescriptionRecord(modelBuilder);
        ConfigurePrescriptionItem(modelBuilder);
        ConfigurePrescriptionTemplate(modelBuilder);
        ConfigurePrescriptionTemplateItem(modelBuilder);
        ConfigurePrescriptionProviderSubmission(modelBuilder);
        ConfigurePrescriptionOutboxRecord(modelBuilder);
        ConfigurePrescriptionEscalation(modelBuilder);
    }

    private static void ConfigureDoctorProviderCredential(ModelBuilder modelBuilder) => modelBuilder.Entity<DoctorProviderCredential>(entity =>
    {
        ConfigureTenantEntity(entity, "DoctorProviderCredentials");
        _ = entity.Property(e => e.ProviderKey).HasMaxLength(64);
        _ = entity.Property(e => e.ProviderDoctorIdentifier).HasMaxLength(256);
        _ = entity.Property(e => e.ProviderClinicIdentifier).HasMaxLength(256);
        _ = entity.Property(e => e.ProviderUsername).HasMaxLength(256);
        _ = entity.Property(e => e.CredentialSecretRef).HasMaxLength(512);
        _ = entity.Property(e => e.TokenSecretRef).HasMaxLength(512);
        _ = entity.Property(e => e.SessionId).HasMaxLength(512);
        _ = entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(32);
        _ = entity.HasIndex(e => new { e.DoctorId, e.ProviderKey, e.ClinicId }).IsUnique()
            .HasDatabaseName("IX_DoctorCredential_Doctor_Provider_Clinic");
    });

    private static void ConfigurePrescriptionRecord(ModelBuilder modelBuilder) => modelBuilder.Entity<PrescriptionRecord>(entity =>
    {
        _ = entity.ToTable("PrescriptionRecords", "Prescription");
        _ = entity.HasQueryFilter(e => e.DeletedAt == null);
        _ = entity.Property(e => e.SelectedProviderKey).HasMaxLength(64);
        _ = entity.Property(e => e.PatientNationalId).HasMaxLength(32);
        _ = entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(32);
        _ = entity.Property(e => e.TenantId).HasMaxLength(128);
        _ = entity.Property(e => e.FallbackReason).HasMaxLength(1024);
        _ = entity.Property(e => e.CancellationReason).HasMaxLength(1024);
        _ = entity.Property(e => e.LastEligibilityStatus).HasConversion<string>().HasMaxLength(64);
        _ = entity.Property(e => e.PaperTrackingNumber).HasMaxLength(256);
        _ = entity.Property(e => e.ReconciliationNotes).HasMaxLength(2048);
        _ = entity.HasIndex(e => new { e.PatientId, e.TenantId }).HasDatabaseName("IX_Prescription_Patient_Tenant");
        _ = entity.HasIndex(e => new { e.DoctorId, e.TenantId, e.CreatedAt }).HasDatabaseName("IX_Prescription_Doctor_Tenant_Created");
        _ = entity.HasIndex(e => new { e.Status, e.TenantId }).HasDatabaseName("IX_Prescription_Status_Tenant");
        _ = entity.IsMultiTenant();
    });

    private static void ConfigurePrescriptionItem(ModelBuilder modelBuilder) => modelBuilder.Entity<PrescriptionItem>(entity =>
    {
        ConfigureTenantEntity(entity, "PrescriptionItems");
        _ = entity.Property(e => e.Category).HasConversion<string>().HasMaxLength(64);
        _ = entity.Property(e => e.ReferenceCode).HasMaxLength(128);
        _ = entity.Property(e => e.DisplayName).HasMaxLength(512);
        _ = entity.Property(e => e.DosageJson).HasMaxLength(4096);
        _ = entity.Property(e => e.ProviderItemIdentifier).HasMaxLength(256);
        _ = entity.Property(e => e.MetadataJson).HasMaxLength(4096);
        _ = entity.Property<Guid>(e => e.PrescriptionRecordId);
        _ = entity.HasOne<PrescriptionRecord>()
            .WithMany(p => p.Items)
            .HasForeignKey(e => e.PrescriptionRecordId)
            .OnDelete(DeleteBehavior.Cascade);
    });

    private static void ConfigurePrescriptionTemplate(ModelBuilder modelBuilder) => modelBuilder.Entity<PrescriptionTemplate>(entity =>
    {
        _ = entity.ToTable("PrescriptionTemplates", "Prescription");
        _ = entity.HasQueryFilter(e => e.DeletedAt == null);
        _ = entity.Property(e => e.TenantId).HasMaxLength(128);
        _ = entity.Property(e => e.Name).HasMaxLength(256);
        _ = entity.Property(e => e.Description).HasMaxLength(1024);
        _ = entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(32);
        _ = entity.HasIndex(e => new { e.TenantId, e.Status, e.Name }).HasDatabaseName("IX_PrescriptionTemplate_Tenant_Status_Name");
        _ = entity.IsMultiTenant();
    });

    private static void ConfigurePrescriptionTemplateItem(ModelBuilder modelBuilder) => modelBuilder.Entity<PrescriptionTemplateItem>(entity =>
    {
        ConfigureTenantEntity(entity, "PrescriptionTemplateItems");
        _ = entity.Property(e => e.Category).HasConversion<string>().HasMaxLength(64);
        _ = entity.Property(e => e.ReferenceCode).HasMaxLength(128);
        _ = entity.Property(e => e.DisplayName).HasMaxLength(512);
        _ = entity.Property(e => e.DosageJson).HasMaxLength(4096);
        _ = entity.Property(e => e.MetadataJson).HasMaxLength(4096);
        _ = entity.HasOne<PrescriptionTemplate>()
            .WithMany(p => p.Items)
            .HasForeignKey(e => e.PrescriptionTemplateId)
            .OnDelete(DeleteBehavior.Cascade);
    });

    private static void ConfigurePrescriptionProviderSubmission(ModelBuilder modelBuilder) => modelBuilder.Entity<PrescriptionProviderSubmission>(entity =>
    {
        ConfigureTenantEntity(entity, "PrescriptionProviderSubmissions");
        _ = entity.Property(e => e.ProviderKey).HasMaxLength(64);
        _ = entity.Property(e => e.OperationName).HasMaxLength(128);
        _ = entity.Property(e => e.RemotePrescriptionId).HasMaxLength(256);
        _ = entity.Property(e => e.RemoteTrackingCode).HasMaxLength(256);
        _ = entity.Property(e => e.RemoteStatusCode).HasMaxLength(128);
        _ = entity.Property(e => e.RemoteStatusText).HasMaxLength(2048);
        _ = entity.Property(e => e.CanonicalStatus).HasConversion<string>().HasMaxLength(32);
        _ = entity.Property(e => e.RawResponseRef).HasMaxLength(1024);
        _ = entity.HasIndex(e => new { e.PrescriptionRecordId, e.ProviderKey }).HasDatabaseName("IX_Submission_Prescription_Provider");
    });

    private static void ConfigurePrescriptionOutboxRecord(ModelBuilder modelBuilder) => modelBuilder.Entity<PrescriptionOutboxRecord>(entity =>
    {
        ConfigureTenantEntity(entity, "PrescriptionOutboxRecords");
        _ = entity.Property(e => e.ProviderKey).HasMaxLength(64);
        _ = entity.Property(e => e.OperationName).HasMaxLength(128);
        _ = entity.Property(e => e.IdempotencyKey).HasMaxLength(512);
        _ = entity.Property(e => e.CorrelationId).HasMaxLength(256);
        _ = entity.Property(e => e.Status).HasMaxLength(32);
        _ = entity.Property(e => e.TenantId).HasMaxLength(128);
        _ = entity.HasIndex(e => new { e.Status, e.NextRetryAt }).HasDatabaseName("IX_OutboxRecord_Status_NextRetry");
        _ = entity.HasIndex(e => e.PrescriptionId).HasDatabaseName("IX_OutboxRecord_PrescriptionId");
    });

    private static void ConfigurePrescriptionEscalation(ModelBuilder modelBuilder) => modelBuilder.Entity<PrescriptionEscalation>(entity =>
    {
        ConfigureTenantEntity(entity, "PrescriptionEscalations");
        _ = entity.Property(e => e.Summary).HasMaxLength(2048);
        _ = entity.Property(e => e.ResolutionNotes).HasMaxLength(2048);
        _ = entity.Property(e => e.StatusAtEscalation).HasConversion<string>().HasMaxLength(32);
        _ = entity.HasIndex(e => e.PrescriptionId).HasDatabaseName("IX_Escalation_PrescriptionId");
        _ = entity.HasIndex(e => new { e.ResolvedAt, e.EscalatedAt }).HasDatabaseName("IX_Escalation_Resolved_Escalated");
    });

    private static void ConfigureTenantEntity<TEntity>(EntityTypeBuilder<TEntity> entity, string tableName)
        where TEntity : class
    {
        _ = entity.ToTable(tableName, "Prescription");
        _ = entity.IsMultiTenant();
    }
}
