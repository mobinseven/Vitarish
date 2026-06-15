using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vitarish.Medical.Infrastructure.Migrations.PrescriptionDb
{
    /// <inheritdoc />
    public partial class PrescriptionInitialization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Prescription");

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
                    TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoctorProviderCredentials", x => x.Id);
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
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
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
                    NextRetryAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastAttemptAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastError = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
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
                    RawResponseRef = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
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
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrescriptionRecords", x => x.Id);
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
                    TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
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

            migrationBuilder.CreateIndex(
                name: "IX_DoctorCredential_Doctor_Provider_Clinic",
                schema: "Prescription",
                table: "DoctorProviderCredentials",
                columns: new[] { "DoctorId", "ProviderKey", "ClinicId" },
                unique: true);

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DoctorProviderCredentials",
                schema: "Prescription");

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
                name: "PrescriptionRecords",
                schema: "Prescription");
        }
    }
}
