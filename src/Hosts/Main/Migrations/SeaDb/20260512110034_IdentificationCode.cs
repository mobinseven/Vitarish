using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vitarish.Main.Migrations.SeaDb
{
    /// <inheritdoc />
    public partial class IdentificationCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IdentificationCode",
                table: "Projects",
                type: "nvarchar(256)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Projects_TenantId_IdentificationCode",
                table: "Projects",
                columns: new[] { "TenantId", "IdentificationCode" },
                unique: true,
                filter: "[IdentificationCode] IS NOT NULL AND [IdentificationCode] != ''");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Projects_TenantId_IdentificationCode",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "IdentificationCode",
                table: "Projects");
        }
    }
}
