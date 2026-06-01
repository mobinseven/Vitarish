using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Vitarish.Prescription.Domain.Entities;
using Vitarish.Prescription.Domain.Enums;

namespace Vitarish.Prescription.Infrastructure.Data;

public sealed class PrescriptionDbContext : DbContext
{
    private readonly IEnumerable<IInterceptor> _interceptors;

    public DbSet<InsuranceProvider> InsuranceProviders => Set<InsuranceProvider>();
    public DbSet<InsuranceProviderCapability> InsuranceProviderCapabilities => Set<InsuranceProviderCapability>();
    public DbSet<ProviderContract> ProviderContracts => Set<ProviderContract>();
    public DbSet<DoctorProviderCredential> DoctorProviderCredentials => Set<DoctorProviderCredential>();
    public DbSet<PatientCoverage> PatientCoverages => Set<PatientCoverage>();
    public DbSet<EligibilityInquiry> EligibilityInquiries => Set<EligibilityInquiry>();
    public DbSet<PrescriptionRecord> Prescriptions => Set<PrescriptionRecord>();
    public DbSet<PrescriptionItem> PrescriptionItems => Set<PrescriptionItem>();
    public DbSet<PrescriptionProviderSubmission> PrescriptionSubmissions => Set<PrescriptionProviderSubmission>();
    public DbSet<ReferenceCode> ReferenceCodes => Set<ReferenceCode>();
    public DbSet<ReferenceCodeCrosswalk> ReferenceCodeCrosswalks => Set<ReferenceCodeCrosswalk>();
    public DbSet<PrescriptionOutboxRecord> OutboxRecords => Set<PrescriptionOutboxRecord>();
    public DbSet<ReferenceCodeSyncJob> ReferenceCodeSyncJobs => Set<ReferenceCodeSyncJob>();
    public DbSet<ReferenceCodeSyncError> ReferenceCodeSyncErrors => Set<ReferenceCodeSyncError>();
    public DbSet<ReferenceCodeChangeLog> ReferenceCodeChangeLogs => Set<ReferenceCodeChangeLog>();

    public PrescriptionDbContext(
        DbContextOptions<PrescriptionDbContext> options,
        IEnumerable<IInterceptor> interceptors) : base(options)
    {
        _interceptors = interceptors;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => _ = optionsBuilder.AddInterceptors(_interceptors);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("Prescription");

        ConfigureInsuranceProvider(modelBuilder);
        ConfigureInsuranceProviderCapability(modelBuilder);
        ConfigureProviderContract(modelBuilder);
        ConfigureDoctorProviderCredential(modelBuilder);
        ConfigurePatientCoverage(modelBuilder);
        ConfigureEligibilityInquiry(modelBuilder);
        ConfigurePrescriptionRecord(modelBuilder);
        ConfigurePrescriptionItem(modelBuilder);
        ConfigurePrescriptionProviderSubmission(modelBuilder);
        ConfigureReferenceCode(modelBuilder);
        ConfigureReferenceCodeCrosswalk(modelBuilder);
        ConfigureOutboxRecord(modelBuilder);
        ConfigureReferenceCodeSyncJob(modelBuilder);
        ConfigureReferenceCodeSyncError(modelBuilder);
        ConfigureReferenceCodeChangeLog(modelBuilder);
    }

    private static void ConfigureInsuranceProvider(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InsuranceProvider>(entity =>
        {
            _ = entity.Property(e => e.ProviderKey).HasMaxLength(64);
            _ = entity.Property(e => e.Name).HasMaxLength(256);
            _ = entity.Property(e => e.ProviderType).HasMaxLength(128);
            _ = entity.Property(e => e.IntegrationMode).HasConversion<string>().HasMaxLength(64);
            _ = entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(32);
            _ = entity.HasIndex(e => e.ProviderKey).IsUnique()
                .HasDatabaseName("IX_InsuranceProvider_ProviderKey");
        });
    }

    private static void ConfigureInsuranceProviderCapability(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InsuranceProviderCapability>(entity =>
        {
            _ = entity.Property(e => e.Capability).HasConversion<string>().HasMaxLength(128);
            _ = entity.Property(e => e.Configuration).HasMaxLength(4096);
            _ = entity.HasIndex(e => new { e.InsuranceProviderId, e.Capability }).IsUnique()
                .HasDatabaseName("IX_ProviderCapability_Provider_Capability");
        });
    }

    private static void ConfigureProviderContract(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProviderContract>(entity =>
        {
            _ = entity.HasIndex(e => new { e.InsuranceProviderId, e.ClinicId }).IsUnique()
                .HasDatabaseName("IX_ProviderContract_Provider_Clinic");
        });
    }

    private static void ConfigureDoctorProviderCredential(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DoctorProviderCredential>(entity =>
        {
            _ = entity.Property(e => e.ProviderDoctorIdentifier).HasMaxLength(256);
            _ = entity.Property(e => e.ProviderUsername).HasMaxLength(256);
            _ = entity.Property(e => e.CredentialSecretRef).HasMaxLength(512);
            _ = entity.Property(e => e.TokenSecretRef).HasMaxLength(512);
            _ = entity.Property(e => e.SessionId).HasMaxLength(512);
            _ = entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(32);
            _ = entity.HasIndex(e => new { e.DoctorId, e.InsuranceProviderId, e.ClinicId }).IsUnique()
                .HasDatabaseName("IX_DoctorCredential_Doctor_Provider_Clinic");
        });
    }

    private static void ConfigurePatientCoverage(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PatientCoverage>(entity =>
        {
            _ = entity.Property(e => e.CoverageIdentifier).HasMaxLength(256);
            _ = entity.Property(e => e.InsuranceBookletIdentifier).HasMaxLength(256);
            _ = entity.Property(e => e.CoverageType).HasMaxLength(128);
            _ = entity.Property(e => e.RawStatusCode).HasMaxLength(128);
            _ = entity.HasIndex(e => new { e.PatientId, e.InsuranceProviderId })
                .HasDatabaseName("IX_PatientCoverage_Patient_Provider");
        });
    }

    private static void ConfigureEligibilityInquiry(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EligibilityInquiry>(entity =>
        {
            _ = entity.Property(e => e.ResultStatus).HasConversion<string>().HasMaxLength(64);
            _ = entity.Property(e => e.TrackingCode).HasMaxLength(256);
            _ = entity.Property(e => e.RawStatusCode).HasMaxLength(128);
            _ = entity.Property(e => e.RawStatusMessage).HasMaxLength(2048);
            _ = entity.HasIndex(e => new { e.PatientId, e.InsuranceProviderId, e.RequestTime })
                .HasDatabaseName("IX_Eligibility_Patient_Provider_Time");
        });
    }

    private static void ConfigurePrescriptionRecord(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PrescriptionRecord>(entity =>
        {
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
            _ = entity.HasIndex(e => new { e.PatientId, e.TenantId })
                .HasDatabaseName("IX_Prescription_Patient_Tenant");
            _ = entity.HasIndex(e => new { e.DoctorId, e.TenantId, e.CreatedAt })
                .HasDatabaseName("IX_Prescription_Doctor_Tenant_Created");
            _ = entity.HasIndex(e => new { e.Status, e.TenantId })
                .HasDatabaseName("IX_Prescription_Status_Tenant");
        });
    }

    private static void ConfigurePrescriptionItem(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PrescriptionItem>(entity =>
        {
            _ = entity.Property(e => e.Category).HasConversion<string>().HasMaxLength(64);
            _ = entity.Property(e => e.ReferenceCode).HasMaxLength(128);
            _ = entity.Property(e => e.DisplayName).HasMaxLength(512);
            _ = entity.Property(e => e.DosageJson).HasMaxLength(4096);
            _ = entity.Property(e => e.ProviderItemIdentifier).HasMaxLength(256);
            _ = entity.Property(e => e.MetadataJson).HasMaxLength(4096);
            _ = entity.HasOne<PrescriptionRecord>()
                .WithMany(p => p.Items)
                .HasForeignKey(e => e.PrescriptionRecordId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigurePrescriptionProviderSubmission(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PrescriptionProviderSubmission>(entity =>
        {
            _ = entity.Property(e => e.OperationName).HasMaxLength(128);
            _ = entity.Property(e => e.RemotePrescriptionId).HasMaxLength(256);
            _ = entity.Property(e => e.RemoteTrackingCode).HasMaxLength(256);
            _ = entity.Property(e => e.RemoteStatusCode).HasMaxLength(128);
            _ = entity.Property(e => e.RemoteStatusText).HasMaxLength(2048);
            _ = entity.Property(e => e.CanonicalStatus).HasConversion<string>().HasMaxLength(32);
            _ = entity.Property(e => e.RawResponseRef).HasMaxLength(1024);
            _ = entity.HasIndex(e => new { e.PrescriptionRecordId, e.InsuranceProviderId })
                .HasDatabaseName("IX_Submission_Prescription_Provider");
        });
    }

    private static void ConfigureReferenceCode(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ReferenceCode>(entity =>
        {
            _ = entity.Property(e => e.ProviderKey).HasMaxLength(64);
            _ = entity.Property(e => e.CodeSystem).HasMaxLength(128);
            _ = entity.Property(e => e.Code).HasMaxLength(128);
            _ = entity.Property(e => e.DisplayName).HasMaxLength(512);
            _ = entity.Property(e => e.Category).HasMaxLength(128);
            _ = entity.Property(e => e.Version).HasMaxLength(128);
            _ = entity.Property(e => e.RecordOrigin).HasMaxLength(64);
            _ = entity.Property(e => e.Checksum).HasMaxLength(256);
            _ = entity.HasIndex(e => new { e.ProviderKey, e.CodeSystem, e.Code, e.RecordOrigin, e.EffectiveFrom })
                .IsUnique()
                .HasDatabaseName("IX_ReferenceCode_Provider_System_Code_Origin_Effective");
            _ = entity.HasIndex(e => new { e.ProviderKey, e.CodeSystem, e.IsActive })
                .HasDatabaseName("IX_ReferenceCode_Provider_System_Active");
        });
    }

    private static void ConfigureReferenceCodeCrosswalk(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ReferenceCodeCrosswalk>(entity =>
        {
            _ = entity.Property(e => e.LocalCode).HasMaxLength(128);
            _ = entity.Property(e => e.LocalCodeSystem).HasMaxLength(128);
            _ = entity.Property(e => e.ProviderKey).HasMaxLength(64);
            _ = entity.Property(e => e.ProviderCode).HasMaxLength(128);
            _ = entity.Property(e => e.ProviderCodeSystem).HasMaxLength(128);
            _ = entity.Property(e => e.MappingConfidence).HasPrecision(5, 4);
            _ = entity.HasIndex(e => new { e.LocalCode, e.LocalCodeSystem, e.ProviderKey, e.ProviderCode, e.ProviderCodeSystem })
                .IsUnique()
                .HasDatabaseName("IX_Crosswalk_Local_Provider_Codes");
        });
    }

    private static void ConfigureOutboxRecord(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PrescriptionOutboxRecord>(entity =>
        {
            _ = entity.Property(e => e.ProviderKey).HasMaxLength(64);
            _ = entity.Property(e => e.OperationName).HasMaxLength(128);
            _ = entity.Property(e => e.IdempotencyKey).HasMaxLength(512);
            _ = entity.Property(e => e.CorrelationId).HasMaxLength(256);
            _ = entity.Property(e => e.Status).HasMaxLength(32);
            _ = entity.Property(e => e.TenantId).HasMaxLength(128);
            _ = entity.HasIndex(e => new { e.Status, e.NextRetryAt })
                .HasDatabaseName("IX_OutboxRecord_Status_NextRetry");
            _ = entity.HasIndex(e => e.PrescriptionId)
                .HasDatabaseName("IX_OutboxRecord_PrescriptionId");
        });
    }

    private static void ConfigureReferenceCodeSyncJob(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ReferenceCodeSyncJob>(entity =>
        {
            _ = entity.Property(e => e.ProviderKey).HasMaxLength(64);
            _ = entity.Property(e => e.CodeSystem).HasMaxLength(128);
            _ = entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(32);
            _ = entity.Property(e => e.Priority).HasConversion<string>().HasMaxLength(16);
            _ = entity.Property(e => e.DataVersion).HasMaxLength(256);
            _ = entity.Property(e => e.PreviousDataVersion).HasMaxLength(256);
            _ = entity.Property(e => e.LastError).HasMaxLength(4096);
            _ = entity.Property(e => e.TriggeredBy).HasMaxLength(256);
            _ = entity.Property(e => e.TenantId).HasMaxLength(128);
            _ = entity.HasIndex(e => new { e.ProviderKey, e.CodeSystem, e.Status })
                .HasDatabaseName("IX_SyncJob_Provider_System_Status");
            _ = entity.HasIndex(e => new { e.ProviderKey, e.CodeSystem, e.CompletedAt })
                .HasDatabaseName("IX_SyncJob_Provider_System_CompletedAt");
        });
    }

    private static void ConfigureReferenceCodeSyncError(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ReferenceCodeSyncError>(entity =>
        {
            _ = entity.Property(e => e.Code).HasMaxLength(128);
            _ = entity.Property(e => e.ErrorMessage).HasMaxLength(2048);
            _ = entity.HasIndex(e => e.SyncJobId)
                .HasDatabaseName("IX_SyncError_SyncJobId");
        });
    }

    private static void ConfigureReferenceCodeChangeLog(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ReferenceCodeChangeLog>(entity =>
        {
            _ = entity.Property(e => e.ChangeType).HasConversion<string>().HasMaxLength(32);
            _ = entity.Property(e => e.Code).HasMaxLength(128);
            _ = entity.Property(e => e.OldValue).HasMaxLength(2048);
            _ = entity.Property(e => e.NewValue).HasMaxLength(2048);
            _ = entity.HasIndex(e => e.SyncJobId)
                .HasDatabaseName("IX_ChangeLog_SyncJobId");
            _ = entity.HasIndex(e => new { e.IsBreakingChange, e.CreatedAt })
                .HasDatabaseName("IX_ChangeLog_Breaking_CreatedAt");
        });
    }
}
