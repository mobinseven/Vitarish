using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vitarish.Medical.Infrastructure.Migrations.PrescriptionDb
{
    public partial class PrescriptionTemplateQueryIndexes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_PrescriptionTemplate_Tenant_Id_Status",
                schema: "Prescription",
                table: "PrescriptionTemplates",
                columns: new[] { "TenantId", "Id", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PrescriptionTemplateItem_Tenant_Template_Sort",
                schema: "Prescription",
                table: "PrescriptionTemplateItems",
                columns: new[] { "TenantId", "PrescriptionTemplateId", "SortOrder" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PrescriptionTemplate_Tenant_Id_Status",
                schema: "Prescription",
                table: "PrescriptionTemplates");

            migrationBuilder.DropIndex(
                name: "IX_PrescriptionTemplateItem_Tenant_Template_Sort",
                schema: "Prescription",
                table: "PrescriptionTemplateItems");
        }
    }
}
