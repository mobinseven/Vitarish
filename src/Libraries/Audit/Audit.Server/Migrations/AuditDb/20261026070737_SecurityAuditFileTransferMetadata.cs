using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vitarish.Audit.Server.Migrations.AuditDb
{
    /// <inheritdoc />
    public partial class SecurityAuditFileTransferMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ResourceExtension",
                schema: "Audit",
                table: "SecurityAuditEvents",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResourceNameHash",
                schema: "Audit",
                table: "SecurityAuditEvents",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ResourceSize",
                schema: "Audit",
                table: "SecurityAuditEvents",
                type: "bigint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ResourceExtension",
                schema: "Audit",
                table: "SecurityAuditEvents");

            migrationBuilder.DropColumn(
                name: "ResourceNameHash",
                schema: "Audit",
                table: "SecurityAuditEvents");

            migrationBuilder.DropColumn(
                name: "ResourceSize",
                schema: "Audit",
                table: "SecurityAuditEvents");
        }
    }
}
