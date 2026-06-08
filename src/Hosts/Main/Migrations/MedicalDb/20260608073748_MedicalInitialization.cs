using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vitarish.Main.Migrations.MedicalDb
{
    /// <inheritdoc />
    public partial class MedicalInitialization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Medical");

            migrationBuilder.EnsureSchema(
                name: "Prescription");

            migrationBuilder.CreateTable(
                name: "CodeSystems",
                schema: "Medical",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SystemKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Authority = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    CanonicalUri = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    Description = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CodeSystems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DoctorProviderCredentials",
                schema: "Prescription",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DoctorId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ClinicId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderDoctorIdentifier = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ProviderUsername = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ProviderClinicIdentifier = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    CredentialSecretRef = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    TokenSecretRef = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    SessionId = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    TokenExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoctorProviderCredentials", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ImportBatches",
                schema: "Medical",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceType = table.Column<int>(type: "integer", nullable: false),
                    SourceVersion = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    SourceLocator = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    SourceChecksum = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ValidationSummary = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: true),
                    ErrorReport = table.Column<string>(type: "text", nullable: true),
                    ImportedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ImportedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportBatches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PrescriptionEscalations",
                schema: "Prescription",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PrescriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    OperatorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Summary = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    ResolutionNotes = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    StatusAtEscalation = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    EscalatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrescriptionEscalations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PrescriptionOutboxRecords",
                schema: "Prescription",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PrescriptionId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProviderKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    OperationName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    PayloadJson = table.Column<string>(type: "text", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    MaxRetries = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    NextRetryAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastAttemptAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastError = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrescriptionOutboxRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PrescriptionProviderSubmissions",
                schema: "Prescription",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PrescriptionRecordId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    OperationName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    RemotePrescriptionId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    RemoteTrackingCode = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    RemoteStatusCode = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    RemoteStatusText = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    SubmissionVersion = table.Column<int>(type: "integer", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastSyncedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CanonicalStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    RawResponseRef = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrescriptionProviderSubmissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PrescriptionRecords",
                schema: "Prescription",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientNationalId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    DoctorId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClinicId = table.Column<Guid>(type: "uuid", nullable: false),
                    EncounterId = table.Column<Guid>(type: "uuid", nullable: true),
                    SelectedProviderKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    VersionNumber = table.Column<int>(type: "integer", nullable: false),
                    LastEligibilityStatus = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    LastEligibilityCheckedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancellationReason = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    FallbackReason = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    PaperTrackingNumber = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ReconciliationNotes = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrescriptionRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CodeSystemReleases",
                schema: "Medical",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CodeSystemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReleaseName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ReleaseDate = table.Column<DateOnly>(type: "date", nullable: true),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: true),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    SourceLocator = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    SourceVersion = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ImportBatchId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CodeSystemReleases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CodeSystemReleases_CodeSystems_CodeSystemId",
                        column: x => x.CodeSystemId,
                        principalSchema: "Medical",
                        principalTable: "CodeSystems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CodeSystemReleases_ImportBatches_ImportBatchId",
                        column: x => x.ImportBatchId,
                        principalSchema: "Medical",
                        principalTable: "ImportBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Drugs",
                schema: "Medical",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PrimaryName = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    NormalizedName = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    PrimaryIdentifierType = table.Column<int>(type: "integer", nullable: false),
                    PrimaryIdentifierValue = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: true),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    RecordOrigin = table.Column<int>(type: "integer", nullable: false),
                    ImportBatchId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Drugs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Drugs_ImportBatches_ImportBatchId",
                        column: x => x.ImportBatchId,
                        principalSchema: "Medical",
                        principalTable: "ImportBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "MedicalServices",
                schema: "Medical",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Category = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Specialty = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Description = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: true),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    RecordOrigin = table.Column<int>(type: "integer", nullable: false),
                    ImportBatchId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicalServices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MedicalServices_ImportBatches_ImportBatchId",
                        column: x => x.ImportBatchId,
                        principalSchema: "Medical",
                        principalTable: "ImportBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "PrescriptionItems",
                schema: "Prescription",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PrescriptionRecordId = table.Column<Guid>(type: "uuid", nullable: false),
                    Category = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ReferenceCode = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    DisplayName = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    DosageJson = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: true),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    DurationDays = table.Column<int>(type: "integer", nullable: true),
                    ProviderItemIdentifier = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    MetadataJson = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrescriptionItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PrescriptionItems_PrescriptionRecords_PrescriptionRecordId",
                        column: x => x.PrescriptionRecordId,
                        principalSchema: "Prescription",
                        principalTable: "PrescriptionRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MedicalCodes",
                schema: "Medical",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CodeSystemReleaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    LongDescription = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: true),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    RecordOrigin = table.Column<int>(type: "integer", nullable: false),
                    SourceMetadataJson = table.Column<string>(type: "text", nullable: true),
                    ImportBatchId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicalCodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MedicalCodes_CodeSystemReleases_CodeSystemReleaseId",
                        column: x => x.CodeSystemReleaseId,
                        principalSchema: "Medical",
                        principalTable: "CodeSystemReleases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MedicalCodes_ImportBatches_ImportBatchId",
                        column: x => x.ImportBatchId,
                        principalSchema: "Medical",
                        principalTable: "ImportBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "DrugProducts",
                schema: "Medical",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DrugId = table.Column<Guid>(type: "uuid", nullable: false),
                    BrandName = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    GenericName = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    DoseForm = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Route = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Strength = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Manufacturer = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    MarketStatus = table.Column<int>(type: "integer", nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: true),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    RecordOrigin = table.Column<int>(type: "integer", nullable: false),
                    ImportBatchId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DrugProducts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DrugProducts_Drugs_DrugId",
                        column: x => x.DrugId,
                        principalSchema: "Medical",
                        principalTable: "Drugs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DrugProducts_ImportBatches_ImportBatchId",
                        column: x => x.ImportBatchId,
                        principalSchema: "Medical",
                        principalTable: "ImportBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ServiceCodes",
                schema: "Medical",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MedicalServiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    CodeSystemReleaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    ModifierApplicabilityJson = table.Column<string>(type: "text", nullable: true),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: true),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    RecordOrigin = table.Column<int>(type: "integer", nullable: false),
                    ImportBatchId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceCodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceCodes_CodeSystemReleases_CodeSystemReleaseId",
                        column: x => x.CodeSystemReleaseId,
                        principalSchema: "Medical",
                        principalTable: "CodeSystemReleases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServiceCodes_ImportBatches_ImportBatchId",
                        column: x => x.ImportBatchId,
                        principalSchema: "Medical",
                        principalTable: "ImportBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ServiceCodes_MedicalServices_MedicalServiceId",
                        column: x => x.MedicalServiceId,
                        principalSchema: "Medical",
                        principalTable: "MedicalServices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CodeMaps",
                schema: "Medical",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceMedicalCodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetMedicalCodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    MappingType = table.Column<int>(type: "integer", nullable: false),
                    Confidence = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: true),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    RecordOrigin = table.Column<int>(type: "integer", nullable: false),
                    ImportBatchId = table.Column<Guid>(type: "uuid", nullable: true),
                    SourceNote = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CodeMaps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CodeMaps_ImportBatches_ImportBatchId",
                        column: x => x.ImportBatchId,
                        principalSchema: "Medical",
                        principalTable: "ImportBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CodeMaps_MedicalCodes_SourceMedicalCodeId",
                        column: x => x.SourceMedicalCodeId,
                        principalSchema: "Medical",
                        principalTable: "MedicalCodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CodeMaps_MedicalCodes_TargetMedicalCodeId",
                        column: x => x.TargetMedicalCodeId,
                        principalSchema: "Medical",
                        principalTable: "MedicalCodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PackagedDrugs",
                schema: "Medical",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DrugProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    PackageIdentifier = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    IdentifierSource = table.Column<int>(type: "integer", nullable: false),
                    NdcCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    PackageSize = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    PackageUnit = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    PackageDescription = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    MarketingStartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    MarketingEndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    SourceReleaseId = table.Column<Guid>(type: "uuid", nullable: true),
                    RecordOrigin = table.Column<int>(type: "integer", nullable: false),
                    ImportBatchId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PackagedDrugs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PackagedDrugs_CodeSystemReleases_SourceReleaseId",
                        column: x => x.SourceReleaseId,
                        principalSchema: "Medical",
                        principalTable: "CodeSystemReleases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PackagedDrugs_DrugProducts_DrugProductId",
                        column: x => x.DrugProductId,
                        principalSchema: "Medical",
                        principalTable: "DrugProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PackagedDrugs_ImportBatches_ImportBatchId",
                        column: x => x.ImportBatchId,
                        principalSchema: "Medical",
                        principalTable: "ImportBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ServiceValuations",
                schema: "Medical",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceCodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ValuationYear = table.Column<int>(type: "integer", nullable: false),
                    Locality = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    FacilityIndicator = table.Column<int>(type: "integer", nullable: false),
                    WorkRvu = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
                    PracticeExpenseRvu = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
                    MalpracticeRvu = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
                    ConversionFactor = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: true),
                    PolicyIndicatorsJson = table.Column<string>(type: "text", nullable: true),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: true),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    SourceReleaseId = table.Column<Guid>(type: "uuid", nullable: true),
                    RecordOrigin = table.Column<int>(type: "integer", nullable: false),
                    ImportBatchId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceValuations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceValuations_CodeSystemReleases_SourceReleaseId",
                        column: x => x.SourceReleaseId,
                        principalSchema: "Medical",
                        principalTable: "CodeSystemReleases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ServiceValuations_ImportBatches_ImportBatchId",
                        column: x => x.ImportBatchId,
                        principalSchema: "Medical",
                        principalTable: "ImportBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ServiceValuations_ServiceCodes_ServiceCodeId",
                        column: x => x.ServiceCodeId,
                        principalSchema: "Medical",
                        principalTable: "ServiceCodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CodeMap_SourceCode",
                schema: "Medical",
                table: "CodeMaps",
                column: "SourceMedicalCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_CodeMap_TargetCode",
                schema: "Medical",
                table: "CodeMaps",
                column: "TargetMedicalCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_CodeMap_Uniqueness",
                schema: "Medical",
                table: "CodeMaps",
                columns: new[] { "SourceMedicalCodeId", "TargetMedicalCodeId", "MappingType", "RecordOrigin", "EffectiveFrom" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CodeMaps_ImportBatchId",
                schema: "Medical",
                table: "CodeMaps",
                column: "ImportBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_CodeSystemRelease_CodeSystem_Release",
                schema: "Medical",
                table: "CodeSystemReleases",
                columns: new[] { "CodeSystemId", "ReleaseName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CodeSystemReleases_ImportBatchId",
                schema: "Medical",
                table: "CodeSystemReleases",
                column: "ImportBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_CodeSystem_SystemKey",
                schema: "Medical",
                table: "CodeSystems",
                column: "SystemKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DoctorCredential_Doctor_Provider_Clinic",
                schema: "Prescription",
                table: "DoctorProviderCredentials",
                columns: new[] { "DoctorId", "ProviderKey", "ClinicId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DrugProducts_DrugId",
                schema: "Medical",
                table: "DrugProducts",
                column: "DrugId");

            migrationBuilder.CreateIndex(
                name: "IX_DrugProducts_ImportBatchId",
                schema: "Medical",
                table: "DrugProducts",
                column: "ImportBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_Drug_Identifier_Origin_Effective",
                schema: "Medical",
                table: "Drugs",
                columns: new[] { "PrimaryIdentifierType", "PrimaryIdentifierValue", "RecordOrigin", "EffectiveFrom" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Drug_NormalizedName",
                schema: "Medical",
                table: "Drugs",
                column: "NormalizedName");

            migrationBuilder.CreateIndex(
                name: "IX_Drugs_ImportBatchId",
                schema: "Medical",
                table: "Drugs",
                column: "ImportBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportBatch_SourceChecksum",
                schema: "Medical",
                table: "ImportBatches",
                column: "SourceChecksum",
                unique: true,
                filter: "\"SourceChecksum\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ImportBatch_SourceType_Version",
                schema: "Medical",
                table: "ImportBatches",
                columns: new[] { "SourceType", "SourceVersion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ImportBatch_Status_Type_ImportedAt",
                schema: "Medical",
                table: "ImportBatches",
                columns: new[] { "Status", "SourceType", "ImportedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MedicalCode_DisplayName",
                schema: "Medical",
                table: "MedicalCodes",
                column: "DisplayName");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalCode_Release_Code_Origin",
                schema: "Medical",
                table: "MedicalCodes",
                columns: new[] { "CodeSystemReleaseId", "Code", "RecordOrigin" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MedicalCodes_ImportBatchId",
                schema: "Medical",
                table: "MedicalCodes",
                column: "ImportBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalServices_ImportBatchId",
                schema: "Medical",
                table: "MedicalServices",
                column: "ImportBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_PackagedDrug_Source_Identifier",
                schema: "Medical",
                table: "PackagedDrugs",
                columns: new[] { "IdentifierSource", "PackageIdentifier" });

            migrationBuilder.CreateIndex(
                name: "IX_PackagedDrug_Source_Identifier_Release",
                schema: "Medical",
                table: "PackagedDrugs",
                columns: new[] { "IdentifierSource", "PackageIdentifier", "SourceReleaseId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PackagedDrugs_DrugProductId",
                schema: "Medical",
                table: "PackagedDrugs",
                column: "DrugProductId");

            migrationBuilder.CreateIndex(
                name: "IX_PackagedDrugs_ImportBatchId",
                schema: "Medical",
                table: "PackagedDrugs",
                column: "ImportBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_PackagedDrugs_SourceReleaseId",
                schema: "Medical",
                table: "PackagedDrugs",
                column: "SourceReleaseId");

            migrationBuilder.CreateIndex(
                name: "IX_Escalation_PrescriptionId",
                schema: "Prescription",
                table: "PrescriptionEscalations",
                column: "PrescriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_Escalation_Resolved_Escalated",
                schema: "Prescription",
                table: "PrescriptionEscalations",
                columns: new[] { "ResolvedAt", "EscalatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PrescriptionItems_PrescriptionRecordId",
                schema: "Prescription",
                table: "PrescriptionItems",
                column: "PrescriptionRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxRecord_PrescriptionId",
                schema: "Prescription",
                table: "PrescriptionOutboxRecords",
                column: "PrescriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxRecord_Status_NextRetry",
                schema: "Prescription",
                table: "PrescriptionOutboxRecords",
                columns: new[] { "Status", "NextRetryAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Submission_Prescription_Provider",
                schema: "Prescription",
                table: "PrescriptionProviderSubmissions",
                columns: new[] { "PrescriptionRecordId", "ProviderKey" });

            migrationBuilder.CreateIndex(
                name: "IX_Prescription_Doctor_Tenant_Created",
                schema: "Prescription",
                table: "PrescriptionRecords",
                columns: new[] { "DoctorId", "TenantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Prescription_Patient_Tenant",
                schema: "Prescription",
                table: "PrescriptionRecords",
                columns: new[] { "PatientId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_Prescription_Status_Tenant",
                schema: "Prescription",
                table: "PrescriptionRecords",
                columns: new[] { "Status", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceCode_Release_Code",
                schema: "Medical",
                table: "ServiceCodes",
                columns: new[] { "CodeSystemReleaseId", "Code" });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceCode_Service_Release_Code_Origin_Effective",
                schema: "Medical",
                table: "ServiceCodes",
                columns: new[] { "MedicalServiceId", "CodeSystemReleaseId", "Code", "RecordOrigin", "EffectiveFrom" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceCodes_ImportBatchId",
                schema: "Medical",
                table: "ServiceCodes",
                column: "ImportBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceValuation_Code_Year_Locality_Facility_Origin_Effective",
                schema: "Medical",
                table: "ServiceValuations",
                columns: new[] { "ServiceCodeId", "ValuationYear", "Locality", "FacilityIndicator", "RecordOrigin", "EffectiveFrom" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceValuation_Lookup",
                schema: "Medical",
                table: "ServiceValuations",
                columns: new[] { "ServiceCodeId", "ValuationYear", "Locality", "FacilityIndicator" });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceValuations_ImportBatchId",
                schema: "Medical",
                table: "ServiceValuations",
                column: "ImportBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceValuations_SourceReleaseId",
                schema: "Medical",
                table: "ServiceValuations",
                column: "SourceReleaseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CodeMaps",
                schema: "Medical");

            migrationBuilder.DropTable(
                name: "DoctorProviderCredentials",
                schema: "Prescription");

            migrationBuilder.DropTable(
                name: "PackagedDrugs",
                schema: "Medical");

            migrationBuilder.DropTable(
                name: "PrescriptionEscalations",
                schema: "Prescription");

            migrationBuilder.DropTable(
                name: "PrescriptionItems",
                schema: "Prescription");

            migrationBuilder.DropTable(
                name: "PrescriptionOutboxRecords",
                schema: "Prescription");

            migrationBuilder.DropTable(
                name: "PrescriptionProviderSubmissions",
                schema: "Prescription");

            migrationBuilder.DropTable(
                name: "ServiceValuations",
                schema: "Medical");

            migrationBuilder.DropTable(
                name: "MedicalCodes",
                schema: "Medical");

            migrationBuilder.DropTable(
                name: "DrugProducts",
                schema: "Medical");

            migrationBuilder.DropTable(
                name: "PrescriptionRecords",
                schema: "Prescription");

            migrationBuilder.DropTable(
                name: "ServiceCodes",
                schema: "Medical");

            migrationBuilder.DropTable(
                name: "Drugs",
                schema: "Medical");

            migrationBuilder.DropTable(
                name: "CodeSystemReleases",
                schema: "Medical");

            migrationBuilder.DropTable(
                name: "MedicalServices",
                schema: "Medical");

            migrationBuilder.DropTable(
                name: "CodeSystems",
                schema: "Medical");

            migrationBuilder.DropTable(
                name: "ImportBatches",
                schema: "Medical");
        }
    }
}
