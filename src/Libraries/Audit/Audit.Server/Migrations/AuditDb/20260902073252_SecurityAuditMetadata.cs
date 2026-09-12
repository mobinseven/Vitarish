using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vitarish.Audit.Server.Migrations.AuditDb
{
    /// <inheritdoc />
    public partial class SecurityAuditMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ActorKind",
                schema: "Audit",
                table: "SecurityAuditEvents",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EventCategory",
                schema: "Audit",
                table: "SecurityAuditEvents",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceIpAddress",
                schema: "Audit",
                table: "SecurityAuditEvents",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceIpKind",
                schema: "Audit",
                table: "SecurityAuditEvents",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActorKind",
                schema: "Audit",
                table: "SecurityAuditEvents");

            migrationBuilder.DropColumn(
                name: "EventCategory",
                schema: "Audit",
                table: "SecurityAuditEvents");

            migrationBuilder.DropColumn(
                name: "SourceIpAddress",
                schema: "Audit",
                table: "SecurityAuditEvents");

            migrationBuilder.DropColumn(
                name: "SourceIpKind",
                schema: "Audit",
                table: "SecurityAuditEvents");
        }
    }
}
