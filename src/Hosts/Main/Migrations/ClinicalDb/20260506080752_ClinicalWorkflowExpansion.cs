using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vitarish.Main.Migrations.ClinicalDb
{
    /// <inheritdoc />
    public partial class ClinicalWorkflowExpansion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClinicalActionRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkflowId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActionType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ActorId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    RequestJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ValidationStatus = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CommitStatus = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    FhirTransactionReference = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ErrorDetailsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OccurredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
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
                    table.PrimaryKey("PK_ClinicalActionRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ClinicalWorkflows",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientFhirReference = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    EncounterFhirReference = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    AppointmentFhirReference = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    VisitType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    AssignedProviderId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    AssignedLocationId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    WorkflowPolicyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_ClinicalWorkflows", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowPolicies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VisitType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PolicyJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    EffectiveFrom = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EffectiveTo = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
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
                    table.PrimaryKey("PK_WorkflowPolicies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowStateHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkflowId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PreviousStatus = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    NewStatus = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ActorId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    OccurredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
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
                    table.PrimaryKey("PK_WorkflowStateHistories", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalActionRecords_WorkflowId_ActionType_OccurredAt",
                table: "ClinicalActionRecords",
                columns: new[] { "WorkflowId", "ActionType", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalWorkflows_PatientFhirReference",
                table: "ClinicalWorkflows",
                column: "PatientFhirReference");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalWorkflows_Status_AssignedLocationId",
                table: "ClinicalWorkflows",
                columns: new[] { "Status", "AssignedLocationId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowPolicies_VisitType_IsActive",
                table: "WorkflowPolicies",
                columns: new[] { "VisitType", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowStateHistories_WorkflowId_OccurredAt",
                table: "WorkflowStateHistories",
                columns: new[] { "WorkflowId", "OccurredAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClinicalActionRecords");

            migrationBuilder.DropTable(
                name: "ClinicalWorkflows");

            migrationBuilder.DropTable(
                name: "WorkflowPolicies");

            migrationBuilder.DropTable(
                name: "WorkflowStateHistories");
        }
    }
}
