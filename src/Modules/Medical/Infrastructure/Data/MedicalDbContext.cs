using Vitarish.Entities.Server.Infrastructure;
namespace Vitarish.Medical.Infrastructure.Data;

public sealed class MedicalDbContext(
    DbContextOptions<MedicalDbContext> options,
    IEnumerable<IInterceptor> interceptors) : DbContext(options)
{
    private readonly IEnumerable<IInterceptor> _interceptors = interceptors;

    public DbSet<CodeSystem> CodeSystems => Set<CodeSystem>();
    public DbSet<CodeSystemRelease> CodeSystemReleases => Set<CodeSystemRelease>();
    public DbSet<MedicalCode> MedicalCodes => Set<MedicalCode>();
    public DbSet<ImportBatch> ImportBatches => Set<ImportBatch>();
    public DbSet<CodeMap> CodeMaps => Set<CodeMap>();
    public DbSet<Drug> Drugs => Set<Drug>();
    public DbSet<DrugProduct> DrugProducts => Set<DrugProduct>();
    public DbSet<PackagedDrug> PackagedDrugs => Set<PackagedDrug>();
    public DbSet<MedicalService> MedicalServices => Set<MedicalService>();
    public DbSet<ServiceCode> ServiceCodes => Set<ServiceCode>();
    public DbSet<ServiceValuation> ServiceValuations => Set<ServiceValuation>();

    // Prescription entities
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
        _ = modelBuilder.ConfigureUtcDateTimeColumns();
        _ = modelBuilder.HasDefaultSchema(nameof(Medical));

        ConfigureCodeSystem(modelBuilder);
        ConfigureCodeSystemRelease(modelBuilder);
        ConfigureMedicalCode(modelBuilder);
        ConfigureImportBatch(modelBuilder);
        ConfigureCodeMap(modelBuilder);
        ConfigureDrug(modelBuilder);
        ConfigureDrugProduct(modelBuilder);
        ConfigurePackagedDrug(modelBuilder);
        ConfigureMedicalService(modelBuilder);
        ConfigureServiceCode(modelBuilder);
        ConfigureServiceValuation(modelBuilder);

        // Prescription entity configurations
        ConfigurePrescriptionDoctorProviderCredential(modelBuilder);
        ConfigurePrescriptionRecord(modelBuilder);
        ConfigurePrescriptionItem(modelBuilder);
        ConfigurePrescriptionProviderSubmission(modelBuilder);
        ConfigurePrescriptionOutboxRecord(modelBuilder);
        ConfigurePrescriptionEscalation(modelBuilder);
    }

    private static void ConfigureCodeSystem(ModelBuilder modelBuilder) => modelBuilder.Entity<CodeSystem>(entity =>
                                                                               {
                                                                                   _ = entity.HasQueryFilter(e => e.DeletedAt == null);
                                                                                   _ = entity.Property(e => e.SystemKey).HasMaxLength(64);
                                                                                   _ = entity.Property(e => e.Name).HasMaxLength(256);
                                                                                   _ = entity.Property(e => e.Authority).HasMaxLength(256);
                                                                                   _ = entity.Property(e => e.CanonicalUri).HasMaxLength(1024);
                                                                                   _ = entity.Property(e => e.Description).HasMaxLength(2048);
                                                                                   _ = entity.HasIndex(e => e.SystemKey).IsUnique()
                                                                                       .HasDatabaseName("IX_CodeSystem_SystemKey");
                                                                               });

    private static void ConfigureCodeSystemRelease(ModelBuilder modelBuilder) => modelBuilder.Entity<CodeSystemRelease>(entity =>
                                                                                      {
                                                                                          _ = entity.HasQueryFilter(e => e.DeletedAt == null);
                                                                                          _ = entity.Property(e => e.ReleaseName).HasMaxLength(128);
                                                                                          _ = entity.Property(e => e.SourceLocator).HasMaxLength(2048);
                                                                                          _ = entity.Property(e => e.SourceVersion).HasMaxLength(128);
                                                                                          _ = entity.HasOne<CodeSystem>()
                                                                                              .WithMany()
                                                                                              .HasForeignKey(e => e.CodeSystemId)
                                                                                              .OnDelete(DeleteBehavior.Restrict);
                                                                                          _ = entity.HasOne<ImportBatch>()
                                                                                              .WithMany()
                                                                                              .HasForeignKey(e => e.ImportBatchId)
                                                                                              .OnDelete(DeleteBehavior.SetNull)
                                                                                              .IsRequired(false);
                                                                                          _ = entity.HasIndex(e => new { e.CodeSystemId, e.ReleaseName }).IsUnique()
                                                                                              .HasDatabaseName("IX_CodeSystemRelease_CodeSystem_Release");
                                                                                      });

    private static void ConfigureMedicalCode(ModelBuilder modelBuilder) => modelBuilder.Entity<MedicalCode>(entity =>
                                                                                {
                                                                                    _ = entity.HasQueryFilter(e => e.DeletedAt == null);
                                                                                    _ = entity.Property(e => e.Code).HasMaxLength(64);
                                                                                    _ = entity.Property(e => e.DisplayName).HasMaxLength(512);
                                                                                    _ = entity.Property(e => e.LongDescription).HasMaxLength(4096);
                                                                                    _ = entity.Property(e => e.SourceMetadataJson);
                                                                                    _ = entity.HasOne<CodeSystemRelease>()
                                                                                        .WithMany()
                                                                                        .HasForeignKey(e => e.CodeSystemReleaseId)
                                                                                        .OnDelete(DeleteBehavior.Restrict);
                                                                                    _ = entity.HasOne<ImportBatch>()
                                                                                        .WithMany()
                                                                                        .HasForeignKey(e => e.ImportBatchId)
                                                                                        .OnDelete(DeleteBehavior.SetNull)
                                                                                        .IsRequired(false);
                                                                                    _ = entity.HasIndex(e => new { e.CodeSystemReleaseId, e.Code, e.RecordOrigin }).IsUnique()
                                                                                        .HasDatabaseName("IX_MedicalCode_Release_Code_Origin");
                                                                                    _ = entity.HasIndex(e => e.DisplayName)
                                                                                        .HasDatabaseName("IX_MedicalCode_DisplayName");
                                                                                });

    private static void ConfigureImportBatch(ModelBuilder modelBuilder) => modelBuilder.Entity<ImportBatch>(entity =>
                                                                                {
                                                                                    _ = entity.HasQueryFilter(e => e.DeletedAt == null);
                                                                                    _ = entity.Property(e => e.SourceVersion).HasMaxLength(128);
                                                                                    _ = entity.Property(e => e.SourceLocator).HasMaxLength(2048);
                                                                                    _ = entity.Property(e => e.SourceChecksum).HasMaxLength(256);
                                                                                    _ = entity.Property(e => e.ImportedBy).HasMaxLength(256);
                                                                                    _ = entity.Property(e => e.ValidationSummary).HasMaxLength(4096);
                                                                                    _ = entity.Property(e => e.ErrorReport);
                                                                                    _ = entity.HasIndex(e => new { e.SourceType, e.SourceVersion }).IsUnique()
                                                                                        .HasDatabaseName("IX_ImportBatch_SourceType_Version");
                                                                                    _ = entity.HasIndex(e => e.SourceChecksum).IsUnique()
                                                                                        .HasFilter("\"SourceChecksum\" IS NOT NULL")
                                                                                        .HasDatabaseName("IX_ImportBatch_SourceChecksum");
                                                                                    _ = entity.HasIndex(e => new { e.Status, e.SourceType, e.ImportedAt })
                                                                                        .HasDatabaseName("IX_ImportBatch_Status_Type_ImportedAt");
                                                                                });

    private static void ConfigureCodeMap(ModelBuilder modelBuilder) => modelBuilder.Entity<CodeMap>(entity =>
                                                                            {
                                                                                _ = entity.HasQueryFilter(e => e.DeletedAt == null);
                                                                                _ = entity.Property(e => e.Confidence).HasPrecision(5, 4);
                                                                                _ = entity.Property(e => e.SourceNote).HasMaxLength(1024);
                                                                                _ = entity.HasOne<MedicalCode>()
                                                                                    .WithMany()
                                                                                    .HasForeignKey(e => e.SourceMedicalCodeId)
                                                                                    .OnDelete(DeleteBehavior.Restrict);
                                                                                _ = entity.HasOne<MedicalCode>()
                                                                                    .WithMany()
                                                                                    .HasForeignKey(e => e.TargetMedicalCodeId)
                                                                                    .OnDelete(DeleteBehavior.Restrict);
                                                                                _ = entity.HasOne<ImportBatch>()
                                                                                    .WithMany()
                                                                                    .HasForeignKey(e => e.ImportBatchId)
                                                                                    .OnDelete(DeleteBehavior.SetNull)
                                                                                    .IsRequired(false);
                                                                                _ = entity.HasIndex(e => e.SourceMedicalCodeId)
                                                                                    .HasDatabaseName("IX_CodeMap_SourceCode");
                                                                                _ = entity.HasIndex(e => e.TargetMedicalCodeId)
                                                                                    .HasDatabaseName("IX_CodeMap_TargetCode");
                                                                                _ = entity.HasIndex(e => new { e.SourceMedicalCodeId, e.TargetMedicalCodeId, e.MappingType, e.RecordOrigin, e.EffectiveFrom })
                                                                                    .IsUnique()
                                                                                    .HasDatabaseName("IX_CodeMap_Uniqueness");
                                                                            });

    private static void ConfigureDrug(ModelBuilder modelBuilder) => modelBuilder.Entity<Drug>(entity =>
                                                                         {
                                                                             _ = entity.HasQueryFilter(e => e.DeletedAt == null);
                                                                             _ = entity.Property(e => e.PrimaryName).HasMaxLength(512);
                                                                             _ = entity.Property(e => e.NormalizedName).HasMaxLength(512);
                                                                             _ = entity.Property(e => e.PrimaryIdentifierValue).HasMaxLength(128);
                                                                             _ = entity.HasOne<ImportBatch>()
                                                                                 .WithMany()
                                                                                 .HasForeignKey(e => e.ImportBatchId)
                                                                                 .OnDelete(DeleteBehavior.SetNull)
                                                                                 .IsRequired(false);
                                                                             _ = entity.HasIndex(e => new { e.PrimaryIdentifierType, e.PrimaryIdentifierValue, e.RecordOrigin, e.EffectiveFrom })
                                                                                 .IsUnique()
                                                                                 .HasDatabaseName("IX_Drug_Identifier_Origin_Effective");
                                                                             _ = entity.HasIndex(e => e.NormalizedName)
                                                                                 .HasDatabaseName("IX_Drug_NormalizedName");
                                                                         });

    private static void ConfigureDrugProduct(ModelBuilder modelBuilder) => modelBuilder.Entity<DrugProduct>(entity =>
                                                                                {
                                                                                    _ = entity.HasQueryFilter(e => e.DeletedAt == null);
                                                                                    _ = entity.Property(e => e.BrandName).HasMaxLength(512);
                                                                                    _ = entity.Property(e => e.GenericName).HasMaxLength(512);
                                                                                    _ = entity.Property(e => e.DoseForm).HasMaxLength(128);
                                                                                    _ = entity.Property(e => e.Route).HasMaxLength(128);
                                                                                    _ = entity.Property(e => e.Strength).HasMaxLength(128);
                                                                                    _ = entity.Property(e => e.Manufacturer).HasMaxLength(256);
                                                                                    _ = entity.HasOne<Drug>()
                                                                                        .WithMany()
                                                                                        .HasForeignKey(e => e.DrugId)
                                                                                        .OnDelete(DeleteBehavior.Restrict);
                                                                                    _ = entity.HasOne<ImportBatch>()
                                                                                        .WithMany()
                                                                                        .HasForeignKey(e => e.ImportBatchId)
                                                                                        .OnDelete(DeleteBehavior.SetNull)
                                                                                        .IsRequired(false);
                                                                                });

    private static void ConfigurePackagedDrug(ModelBuilder modelBuilder) => modelBuilder.Entity<PackagedDrug>(entity =>
                                                                                 {
                                                                                     _ = entity.HasQueryFilter(e => e.DeletedAt == null);
                                                                                     _ = entity.Property(e => e.PackageIdentifier).HasMaxLength(128);
                                                                                     _ = entity.Property(e => e.NdcCode).HasMaxLength(64);
                                                                                     _ = entity.Property(e => e.PackageSize).HasMaxLength(64);
                                                                                     _ = entity.Property(e => e.PackageUnit).HasMaxLength(64);
                                                                                     _ = entity.Property(e => e.PackageDescription).HasMaxLength(512);
                                                                                     _ = entity.HasOne<DrugProduct>()
                                                                                         .WithMany()
                                                                                         .HasForeignKey(e => e.DrugProductId)
                                                                                         .OnDelete(DeleteBehavior.Restrict);
                                                                                     _ = entity.HasOne<CodeSystemRelease>()
                                                                                         .WithMany()
                                                                                         .HasForeignKey(e => e.SourceReleaseId)
                                                                                         .OnDelete(DeleteBehavior.SetNull)
                                                                                         .IsRequired(false);
                                                                                     _ = entity.HasOne<ImportBatch>()
                                                                                         .WithMany()
                                                                                         .HasForeignKey(e => e.ImportBatchId)
                                                                                         .OnDelete(DeleteBehavior.SetNull)
                                                                                         .IsRequired(false);
                                                                                     _ = entity.HasIndex(e => new { e.IdentifierSource, e.PackageIdentifier, e.SourceReleaseId })
                                                                                         .IsUnique()
                                                                                         .HasDatabaseName("IX_PackagedDrug_Source_Identifier_Release");
                                                                                     _ = entity.HasIndex(e => new { e.IdentifierSource, e.PackageIdentifier })
                                                                                         .HasDatabaseName("IX_PackagedDrug_Source_Identifier");
                                                                                 });

    private static void ConfigureMedicalService(ModelBuilder modelBuilder) => modelBuilder.Entity<MedicalService>(entity =>
                                                                                   {
                                                                                       _ = entity.HasQueryFilter(e => e.DeletedAt == null);
                                                                                       _ = entity.Property(e => e.Name).HasMaxLength(512);
                                                                                       _ = entity.Property(e => e.Category).HasMaxLength(128);
                                                                                       _ = entity.Property(e => e.Specialty).HasMaxLength(128);
                                                                                       _ = entity.Property(e => e.Description).HasMaxLength(2048);
                                                                                       _ = entity.HasOne<ImportBatch>()
                                                                                           .WithMany()
                                                                                           .HasForeignKey(e => e.ImportBatchId)
                                                                                           .OnDelete(DeleteBehavior.SetNull)
                                                                                           .IsRequired(false);
                                                                                   });

    private static void ConfigureServiceCode(ModelBuilder modelBuilder) => modelBuilder.Entity<ServiceCode>(entity =>
                                                                                {
                                                                                    _ = entity.HasQueryFilter(e => e.DeletedAt == null);
                                                                                    _ = entity.Property(e => e.Code).HasMaxLength(64);
                                                                                    _ = entity.Property(e => e.DisplayName).HasMaxLength(512);
                                                                                    _ = entity.Property(e => e.ModifierApplicabilityJson);
                                                                                    _ = entity.HasOne<MedicalService>()
                                                                                        .WithMany()
                                                                                        .HasForeignKey(e => e.MedicalServiceId)
                                                                                        .OnDelete(DeleteBehavior.Restrict);
                                                                                    _ = entity.HasOne<CodeSystemRelease>()
                                                                                        .WithMany()
                                                                                        .HasForeignKey(e => e.CodeSystemReleaseId)
                                                                                        .OnDelete(DeleteBehavior.Restrict);
                                                                                    _ = entity.HasOne<ImportBatch>()
                                                                                        .WithMany()
                                                                                        .HasForeignKey(e => e.ImportBatchId)
                                                                                        .OnDelete(DeleteBehavior.SetNull)
                                                                                        .IsRequired(false);
                                                                                    _ = entity.HasIndex(e => new { e.MedicalServiceId, e.CodeSystemReleaseId, e.Code, e.RecordOrigin, e.EffectiveFrom })
                                                                                        .IsUnique()
                                                                                        .HasDatabaseName("IX_ServiceCode_Service_Release_Code_Origin_Effective");
                                                                                    _ = entity.HasIndex(e => new { e.CodeSystemReleaseId, e.Code })
                                                                                        .HasDatabaseName("IX_ServiceCode_Release_Code");
                                                                                });

    private static void ConfigureServiceValuation(ModelBuilder modelBuilder) => modelBuilder.Entity<ServiceValuation>(entity =>
                                                                                     {
                                                                                         _ = entity.HasQueryFilter(e => e.DeletedAt == null);
                                                                                         _ = entity.Property(e => e.Locality).HasMaxLength(64);
                                                                                         _ = entity.Property(e => e.WorkRvu).HasPrecision(10, 4);
                                                                                         _ = entity.Property(e => e.PracticeExpenseRvu).HasPrecision(10, 4);
                                                                                         _ = entity.Property(e => e.MalpracticeRvu).HasPrecision(10, 4);
                                                                                         _ = entity.Property(e => e.ConversionFactor).HasPrecision(10, 4);
                                                                                         _ = entity.Property(e => e.PolicyIndicatorsJson);
                                                                                         _ = entity.HasOne<ServiceCode>()
                                                                                             .WithMany()
                                                                                             .HasForeignKey(e => e.ServiceCodeId)
                                                                                             .OnDelete(DeleteBehavior.Restrict);
                                                                                         _ = entity.HasOne<CodeSystemRelease>()
                                                                                             .WithMany()
                                                                                             .HasForeignKey(e => e.SourceReleaseId)
                                                                                             .OnDelete(DeleteBehavior.SetNull)
                                                                                             .IsRequired(false);
                                                                                         _ = entity.HasOne<ImportBatch>()
                                                                                             .WithMany()
                                                                                             .HasForeignKey(e => e.ImportBatchId)
                                                                                             .OnDelete(DeleteBehavior.SetNull)
                                                                                             .IsRequired(false);
                                                                                         _ = entity.HasIndex(e => new { e.ServiceCodeId, e.ValuationYear, e.Locality, e.FacilityIndicator, e.RecordOrigin, e.EffectiveFrom })
                                                                                             .IsUnique()
                                                                                             .HasDatabaseName("IX_ServiceValuation_Code_Year_Locality_Facility_Origin_Effective");
                                                                                         _ = entity.HasIndex(e => new { e.ServiceCodeId, e.ValuationYear, e.Locality, e.FacilityIndicator })
                                                                                             .HasDatabaseName("IX_ServiceValuation_Lookup");
                                                                                     });

    private static void ConfigurePrescriptionDoctorProviderCredential(ModelBuilder modelBuilder) => modelBuilder.Entity<DoctorProviderCredential>(entity =>
                                                                                             {
                                                                                                 _ = entity.ToTable("DoctorProviderCredentials", "Prescription");
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
                                                                                           _ = entity.HasIndex(e => new { e.PatientId, e.TenantId })
                                                                                               .HasDatabaseName("IX_Prescription_Patient_Tenant");
                                                                                           _ = entity.HasIndex(e => new { e.DoctorId, e.TenantId, e.CreatedAt })
                                                                                               .HasDatabaseName("IX_Prescription_Doctor_Tenant_Created");
                                                                                           _ = entity.HasIndex(e => new { e.Status, e.TenantId })
                                                                                               .HasDatabaseName("IX_Prescription_Status_Tenant");
                                                                                       });

    private static void ConfigurePrescriptionItem(ModelBuilder modelBuilder) => modelBuilder.Entity<PrescriptionItem>(entity =>
                                                                                     {
                                                                                         _ = entity.ToTable("PrescriptionItems", "Prescription");
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

    private static void ConfigurePrescriptionProviderSubmission(ModelBuilder modelBuilder) => modelBuilder.Entity<PrescriptionProviderSubmission>(entity =>
                                                                                                   {
                                                                                                       _ = entity.ToTable("PrescriptionProviderSubmissions", "Prescription");
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

    private static void ConfigurePrescriptionOutboxRecord(ModelBuilder modelBuilder) => modelBuilder.Entity<PrescriptionOutboxRecord>(entity =>
                                                                                                 {
                                                                                                     _ = entity.ToTable("PrescriptionOutboxRecords", "Prescription");
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

    private static void ConfigurePrescriptionEscalation(ModelBuilder modelBuilder) => modelBuilder.Entity<PrescriptionEscalation>(entity =>
                                                                                               {
                                                                                                   _ = entity.ToTable("PrescriptionEscalations", "Prescription");
                                                                                                   _ = entity.Property(e => e.Summary).HasMaxLength(2048);
                                                                                                   _ = entity.Property(e => e.ResolutionNotes).HasMaxLength(2048);
                                                                                                   _ = entity.Property(e => e.StatusAtEscalation).HasConversion<string>().HasMaxLength(32);
                                                                                                   _ = entity.HasIndex(e => e.PrescriptionId)
                                                                                                       .HasDatabaseName("IX_Escalation_PrescriptionId");
                                                                                                   _ = entity.HasIndex(e => new { e.ResolvedAt, e.EscalatedAt })
                                                                                                       .HasDatabaseName("IX_Escalation_Resolved_Escalated");
                                                                                               });
}
