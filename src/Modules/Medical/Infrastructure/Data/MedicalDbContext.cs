namespace Vitarish.Medical.Infrastructure.Data;

public sealed class MedicalDbContext : DbContext
{
    private readonly IEnumerable<IInterceptor> _interceptors;

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

    public MedicalDbContext(
        DbContextOptions<MedicalDbContext> options,
        IEnumerable<IInterceptor> interceptors) : base(options)
    {
        _interceptors = interceptors;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => _ = optionsBuilder.AddInterceptors(_interceptors);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("Medical");

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
    }

    private static void ConfigureCodeSystem(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CodeSystem>(entity =>
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
    }

    private static void ConfigureCodeSystemRelease(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CodeSystemRelease>(entity =>
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
    }

    private static void ConfigureMedicalCode(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MedicalCode>(entity =>
        {
            _ = entity.HasQueryFilter(e => e.DeletedAt == null);
            _ = entity.Property(e => e.Code).HasMaxLength(64);
            _ = entity.Property(e => e.DisplayName).HasMaxLength(512);
            _ = entity.Property(e => e.LongDescription).HasMaxLength(4096);
            _ = entity.Property(e => e.SourceMetadataJson).HasColumnType("nvarchar(max)");
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
    }

    private static void ConfigureImportBatch(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ImportBatch>(entity =>
        {
            _ = entity.HasQueryFilter(e => e.DeletedAt == null);
            _ = entity.Property(e => e.SourceVersion).HasMaxLength(128);
            _ = entity.Property(e => e.SourceLocator).HasMaxLength(2048);
            _ = entity.Property(e => e.SourceChecksum).HasMaxLength(256);
            _ = entity.Property(e => e.ImportedBy).HasMaxLength(256);
            _ = entity.Property(e => e.ValidationSummary).HasMaxLength(4096);
            _ = entity.Property(e => e.ErrorReport).HasColumnType("nvarchar(max)");
            _ = entity.HasIndex(e => new { e.SourceType, e.SourceVersion }).IsUnique()
                .HasDatabaseName("IX_ImportBatch_SourceType_Version");
            _ = entity.HasIndex(e => e.SourceChecksum).IsUnique()
                .HasFilter("[SourceChecksum] IS NOT NULL")
                .HasDatabaseName("IX_ImportBatch_SourceChecksum");
            _ = entity.HasIndex(e => new { e.Status, e.SourceType, e.ImportedAt })
                .HasDatabaseName("IX_ImportBatch_Status_Type_ImportedAt");
        });
    }

    private static void ConfigureCodeMap(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CodeMap>(entity =>
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
                .HasFilter("[EffectiveFrom] IS NOT NULL")
                .HasDatabaseName("IX_CodeMap_Uniqueness");
        });
    }

    private static void ConfigureDrug(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Drug>(entity =>
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
                .HasFilter("[EffectiveFrom] IS NOT NULL")
                .HasDatabaseName("IX_Drug_Identifier_Origin_Effective");
            _ = entity.HasIndex(e => e.NormalizedName)
                .HasDatabaseName("IX_Drug_NormalizedName");
        });
    }

    private static void ConfigureDrugProduct(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DrugProduct>(entity =>
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
    }

    private static void ConfigurePackagedDrug(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PackagedDrug>(entity =>
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
    }

    private static void ConfigureMedicalService(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MedicalService>(entity =>
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
    }

    private static void ConfigureServiceCode(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ServiceCode>(entity =>
        {
            _ = entity.HasQueryFilter(e => e.DeletedAt == null);
            _ = entity.Property(e => e.Code).HasMaxLength(64);
            _ = entity.Property(e => e.DisplayName).HasMaxLength(512);
            _ = entity.Property(e => e.ModifierApplicabilityJson).HasColumnType("nvarchar(max)");
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
                .HasFilter("[EffectiveFrom] IS NOT NULL")
                .HasDatabaseName("IX_ServiceCode_Service_Release_Code_Origin_Effective");
            _ = entity.HasIndex(e => new { e.CodeSystemReleaseId, e.Code })
                .HasDatabaseName("IX_ServiceCode_Release_Code");
        });
    }

    private static void ConfigureServiceValuation(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ServiceValuation>(entity =>
        {
            _ = entity.HasQueryFilter(e => e.DeletedAt == null);
            _ = entity.Property(e => e.Locality).HasMaxLength(64);
            _ = entity.Property(e => e.WorkRvu).HasPrecision(10, 4);
            _ = entity.Property(e => e.PracticeExpenseRvu).HasPrecision(10, 4);
            _ = entity.Property(e => e.MalpracticeRvu).HasPrecision(10, 4);
            _ = entity.Property(e => e.ConversionFactor).HasPrecision(10, 4);
            _ = entity.Property(e => e.PolicyIndicatorsJson).HasColumnType("nvarchar(max)");
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
                .HasFilter("[EffectiveFrom] IS NOT NULL")
                .HasDatabaseName("IX_ServiceValuation_Code_Year_Locality_Facility_Origin_Effective");
            _ = entity.HasIndex(e => new { e.ServiceCodeId, e.ValuationYear, e.Locality, e.FacilityIndicator })
                .HasDatabaseName("IX_ServiceValuation_Lookup");
        });
    }
}
