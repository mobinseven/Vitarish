using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vitarish.Audit.Server.Migrations.AuditDb
{
    /// <inheritdoc />
    public partial class AddSecurityAuditEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SecurityAuditEvents",
                schema: "Audit",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Outcome = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ActorId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    SubjectId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Resource = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Operation = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    SourceBoundary = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    RedactedReason = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    IdempotencyKey = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecurityAuditEvents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAuditEvents_OccurredAt",
                schema: "Audit",
                table: "SecurityAuditEvents",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAuditEvents_Resource_OccurredAt",
                schema: "Audit",
                table: "SecurityAuditEvents",
                columns: new[] { "Resource", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAuditEvents_TenantId_IdempotencyKey",
                schema: "Audit",
                table: "SecurityAuditEvents",
                columns: new[] { "TenantId", "IdempotencyKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SecurityAuditEvents",
                schema: "Audit");
        }
    }
}
