using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vitarish.Medical.Infrastructure.Migrations.MedicalDb
{
    /// <inheritdoc />
    public partial class ProviderReferenceSearch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NormalizedCode",
                schema: "Medical",
                table: "MedicalCodes",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NormalizedDisplayName",
                schema: "Medical",
                table: "MedicalCodes",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NormalizedProviderCategory",
                schema: "Medical",
                table: "MedicalCodes",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderCategory",
                schema: "Medical",
                table: "MedicalCodes",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "Medical"."MedicalCodes"
                SET "NormalizedCode" = UPPER(TRIM("Code")),
                    "NormalizedDisplayName" = UPPER(TRIM("DisplayName"))
                WHERE "NormalizedCode" = '' OR "NormalizedDisplayName" = '';
                """);

            migrationBuilder.CreateIndex(
                name: "IX_MedicalCode_ProviderCategory",
                schema: "Medical",
                table: "MedicalCodes",
                columns: new[] { "CodeSystemReleaseId", "NormalizedProviderCategory" });

            migrationBuilder.CreateIndex(
                name: "IX_MedicalCode_ReferenceSearch",
                schema: "Medical",
                table: "MedicalCodes",
                columns: new[] { "CodeSystemReleaseId", "NormalizedCode", "NormalizedDisplayName" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MedicalCode_ProviderCategory",
                schema: "Medical",
                table: "MedicalCodes");

            migrationBuilder.DropIndex(
                name: "IX_MedicalCode_ReferenceSearch",
                schema: "Medical",
                table: "MedicalCodes");

            migrationBuilder.DropColumn(
                name: "NormalizedCode",
                schema: "Medical",
                table: "MedicalCodes");

            migrationBuilder.DropColumn(
                name: "NormalizedDisplayName",
                schema: "Medical",
                table: "MedicalCodes");

            migrationBuilder.DropColumn(
                name: "NormalizedProviderCategory",
                schema: "Medical",
                table: "MedicalCodes");

            migrationBuilder.DropColumn(
                name: "ProviderCategory",
                schema: "Medical",
                table: "MedicalCodes");
        }
    }
}
