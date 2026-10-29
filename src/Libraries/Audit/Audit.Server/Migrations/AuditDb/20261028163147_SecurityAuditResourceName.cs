using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vitarish.Audit.Server.Migrations.AuditDb
{
    /// <inheritdoc />
    public partial class SecurityAuditResourceName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ResourceName",
                schema: "Audit",
                table: "SecurityAuditEvents",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ResourceName",
                schema: "Audit",
                table: "SecurityAuditEvents");
        }
    }
}
