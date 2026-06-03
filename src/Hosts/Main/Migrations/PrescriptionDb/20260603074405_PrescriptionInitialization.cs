using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vitarish.Main.Migrations.PrescriptionDb
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
                    CredentialSecretRef = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    TokenSecretRef = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    SessionId = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    TokenExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoctorProviderCredentials", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Escalations",
                schema: "Prescription",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PrescriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    OperatorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Summary = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    ResolutionNotes = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    StatusAtEscalation = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    EscalatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Escalations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OutboxRecords",
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
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    NextRetryAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastAttemptAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastError = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Prescriptions",
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
                    LastEligibilityCheckedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CancellationReason = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    FallbackReason = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    PaperTrackingNumber = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ReconciliationNotes = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Prescriptions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PrescriptionSubmissions",
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
                    SubmittedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastSyncedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CanonicalStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    RawResponseRef = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrescriptionSubmissions", x => x.Id);
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
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrescriptionItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PrescriptionItems_Prescriptions_PrescriptionRecordId",
                        column: x => x.PrescriptionRecordId,
                        principalSchema: "Prescription",
                        principalTable: "Prescriptions",
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
                table: "Escalations",
                column: "PrescriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_Escalation_Resolved_Escalated",
                schema: "Prescription",
                table: "Escalations",
                columns: new[] { "ResolvedAt", "EscalatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxRecord_PrescriptionId",
                schema: "Prescription",
                table: "OutboxRecords",
                column: "PrescriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxRecord_Status_NextRetry",
                schema: "Prescription",
                table: "OutboxRecords",
                columns: new[] { "Status", "NextRetryAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PrescriptionItems_PrescriptionRecordId",
                schema: "Prescription",
                table: "PrescriptionItems",
                column: "PrescriptionRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_Prescription_Doctor_Tenant_Created",
                schema: "Prescription",
                table: "Prescriptions",
                columns: new[] { "DoctorId", "TenantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Prescription_Patient_Tenant",
                schema: "Prescription",
                table: "Prescriptions",
                columns: new[] { "PatientId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_Prescription_Status_Tenant",
                schema: "Prescription",
                table: "Prescriptions",
                columns: new[] { "Status", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_Submission_Prescription_Provider",
                schema: "Prescription",
                table: "PrescriptionSubmissions",
                columns: new[] { "PrescriptionRecordId", "ProviderKey" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DoctorProviderCredentials",
                schema: "Prescription");

            migrationBuilder.DropTable(
                name: "Escalations",
                schema: "Prescription");

            migrationBuilder.DropTable(
                name: "OutboxRecords",
                schema: "Prescription");

            migrationBuilder.DropTable(
                name: "PrescriptionItems",
                schema: "Prescription");

            migrationBuilder.DropTable(
                name: "PrescriptionSubmissions",
                schema: "Prescription");

            migrationBuilder.DropTable(
                name: "Prescriptions",
                schema: "Prescription");
        }
    }
}
