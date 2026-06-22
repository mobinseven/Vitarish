using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vitarish.Medical.Infrastructure.Migrations.PrescriptionDb
{
    public partial class PrescriptionTemplateQueryIndexes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_PrescriptionTemplateItem_Tenant_Template_Sort",
                schema: "Prescription",
                table: "PrescriptionTemplateItems",
                columns: new[] { "TenantId", "PrescriptionTemplateId", "SortOrder" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PrescriptionTemplateItem_Tenant_Template_Sort",
                schema: "Prescription",
                table: "PrescriptionTemplateItems");
        }
    }
}
