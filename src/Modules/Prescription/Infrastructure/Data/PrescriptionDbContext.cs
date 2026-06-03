using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Vitarish.Prescription.Domain.Entities;

namespace Vitarish.Prescription.Infrastructure.Data;

public sealed class PrescriptionDbContext(
    DbContextOptions<PrescriptionDbContext> options,
    IEnumerable<IInterceptor> interceptors) : DbContext(options)
{
    private readonly IEnumerable<IInterceptor> _interceptors = interceptors;

    public DbSet<DoctorProviderCredential> DoctorProviderCredentials => Set<DoctorProviderCredential>();
    public DbSet<PrescriptionRecord> Prescriptions => Set<PrescriptionRecord>();
    public DbSet<PrescriptionItem> PrescriptionItems => Set<PrescriptionItem>();
    public DbSet<PrescriptionProviderSubmission> PrescriptionSubmissions => Set<PrescriptionProviderSubmission>();
    public DbSet<PrescriptionOutboxRecord> OutboxRecords => Set<PrescriptionOutboxRecord>();
    public DbSet<PrescriptionEscalation> Escalations => Set<PrescriptionEscalation>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => _ = optionsBuilder.AddInterceptors(_interceptors);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        _ = modelBuilder.HasDefaultSchema(nameof(Prescription));

        ConfigureDoctorProviderCredential(modelBuilder);
        ConfigurePrescriptionRecord(modelBuilder);
        ConfigurePrescriptionItem(modelBuilder);
        ConfigurePrescriptionProviderSubmission(modelBuilder);
        ConfigureOutboxRecord(modelBuilder);
        ConfigureEscalation(modelBuilder);
    }

    private static void ConfigureDoctorProviderCredential(ModelBuilder modelBuilder) => modelBuilder.Entity<DoctorProviderCredential>(entity =>
                                                                                             {
                                                                                                 _ = entity.Property(e => e.ProviderKey).HasMaxLength(64);
                                                                                                 _ = entity.Property(e => e.ProviderDoctorIdentifier).HasMaxLength(256);
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

    private static void ConfigurePrescriptionItem(ModelBuilder modelBuilder) => modelBuilder.Entity<PrescriptionItem>(entity =>
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

    private static void ConfigurePrescriptionProviderSubmission(ModelBuilder modelBuilder) => modelBuilder.Entity<PrescriptionProviderSubmission>(entity =>
                                                                                                   {
                                                                                                       _ = entity.Property(e => e.ProviderKey).HasMaxLength(64);
                                                                                                       _ = entity.Property(e => e.OperationName).HasMaxLength(128);
                                                                                                       _ = entity.Property(e => e.RemotePrescriptionId).HasMaxLength(256);
                                                                                                       _ = entity.Property(e => e.RemoteTrackingCode).HasMaxLength(256);
                                                                                                       _ = entity.Property(e => e.RemoteStatusCode).HasMaxLength(128);
                                                                                                       _ = entity.Property(e => e.RemoteStatusText).HasMaxLength(2048);
                                                                                                       _ = entity.Property(e => e.CanonicalStatus).HasConversion<string>().HasMaxLength(32);
                                                                                                       _ = entity.Property(e => e.RawResponseRef).HasMaxLength(1024);
                                                                                                       _ = entity.HasIndex(e => new { e.PrescriptionRecordId, e.ProviderKey })
                                                                                                           .HasDatabaseName("IX_Submission_Prescription_Provider");
                                                                                                   });

    private static void ConfigureOutboxRecord(ModelBuilder modelBuilder) => modelBuilder.Entity<PrescriptionOutboxRecord>(entity =>
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

    private static void ConfigureEscalation(ModelBuilder modelBuilder) => modelBuilder.Entity<PrescriptionEscalation>(entity =>
                                                                               {
                                                                                   _ = entity.Property(e => e.Summary).HasMaxLength(2048);
                                                                                   _ = entity.Property(e => e.ResolutionNotes).HasMaxLength(2048);
                                                                                   _ = entity.Property(e => e.StatusAtEscalation).HasConversion<string>().HasMaxLength(32);
                                                                                   _ = entity.HasIndex(e => e.PrescriptionId)
                                                                                       .HasDatabaseName("IX_Escalation_PrescriptionId");
                                                                                   _ = entity.HasIndex(e => new { e.ResolvedAt, e.EscalatedAt })
                                                                                       .HasDatabaseName("IX_Escalation_Resolved_Escalated");
                                                                               });
}
