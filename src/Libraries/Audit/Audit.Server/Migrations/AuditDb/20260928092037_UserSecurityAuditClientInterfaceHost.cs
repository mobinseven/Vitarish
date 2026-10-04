using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vitarish.Audit.Server.Migrations.AuditDb
{
    /// <inheritdoc />
    public partial class UserSecurityAuditClientInterfaceHost : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClientInterfaceHost",
                schema: "Audit",
                table: "SecurityAuditEvents",
                type: "character varying(253)",
                maxLength: 253,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAuditEvents_TenantId_SubjectId_OccurredAt",
                schema: "Audit",
                table: "SecurityAuditEvents",
                columns: new[] { "TenantId", "SubjectId", "OccurredAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SecurityAuditEvents_TenantId_SubjectId_OccurredAt",
                schema: "Audit",
                table: "SecurityAuditEvents");

            migrationBuilder.DropColumn(
                name: "ClientInterfaceHost",
                schema: "Audit",
                table: "SecurityAuditEvents");
        }
    }
}
