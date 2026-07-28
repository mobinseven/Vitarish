using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vitarish.Medical.Infrastructure.Migrations.PrescriptionDb
{
    /// <inheritdoc />
    public partial class PrescriptionEncounterFeedIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Prescription_Tenant_Encounter",
                schema: "Prescription",
                table: "PrescriptionRecords",
                columns: new[] { "TenantId", "EncounterId" },
                filter: "\"EncounterId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Prescription_Tenant_Encounter",
                schema: "Prescription",
                table: "PrescriptionRecords");
        }
    }
}
