using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vitarish.Main.Migrations.ClinicalDb
{
    /// <inheritdoc />
    public partial class ClinicalInitialization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ActorId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    ResourceType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    ResourceId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    Outcome = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    DetailsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ConnectorHealthSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Endpoint = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    State = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    IsReachable = table.Column<bool>(type: "bit", nullable: false),
                    FhirVersion = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    SoftwareName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    SoftwareVersion = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    FailureMessage = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    SupportedResourceTypes = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    CheckedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConnectorHealthSnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FhirOperations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResourceType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ResourceId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Interaction = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    FailureKind = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CorrelationId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    RequestJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResponseJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OutcomeJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FailureMessage = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    ResponseStatusCode = table.Column<int>(type: "int", nullable: true),
                    ResultCount = table.Column<int>(type: "int", nullable: true),
                    RemoteVersionId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    ETag = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FhirOperations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ResourceLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LocalEntityType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    LocalEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResourceType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ResourceId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ResourceVersionId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    ETag = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    OwnerUserId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    PatientReference = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false),
                    ArchivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceLinks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ResourceProjections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResourceLinkId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResourceType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ResourceId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PatientReference = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ClinicReference = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    SourceVersionId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    LastSyncedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    SyncState = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceProjections", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEntries_Category_Action_ResourceType_ResourceId",
                table: "AuditEntries",
                columns: new[] { "Category", "Action", "ResourceType", "ResourceId" });

            migrationBuilder.CreateIndex(
                name: "IX_ConnectorHealthSnapshots_CheckedAt",
                table: "ConnectorHealthSnapshots",
                column: "CheckedAt");

            migrationBuilder.CreateIndex(
                name: "IX_FhirOperations_CorrelationId_StartedAt",
                table: "FhirOperations",
                columns: new[] { "CorrelationId", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FhirOperations_IdempotencyKey_ResourceType_Interaction",
                table: "FhirOperations",
                columns: new[] { "IdempotencyKey", "ResourceType", "Interaction" });

            migrationBuilder.CreateIndex(
                name: "IX_ResourceLinks_LocalEntityType_LocalEntityId_ResourceType_ResourceId",
                table: "ResourceLinks",
                columns: new[] { "LocalEntityType", "LocalEntityId", "ResourceType", "ResourceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResourceProjections_ResourceType_ResourceId",
                table: "ResourceProjections",
                columns: new[] { "ResourceType", "ResourceId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditEntries");

            migrationBuilder.DropTable(
                name: "ConnectorHealthSnapshots");

            migrationBuilder.DropTable(
                name: "FhirOperations");

            migrationBuilder.DropTable(
                name: "ResourceLinks");

            migrationBuilder.DropTable(
                name: "ResourceProjections");
        }
    }
}
