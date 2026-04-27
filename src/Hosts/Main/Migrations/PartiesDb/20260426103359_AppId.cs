using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vitarish.Main.Migrations.PartiesDb
{
    /// <inheritdoc />
    public partial class AppId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "EstateId",
                table: "Tenants",
                newName: "AppId");

            migrationBuilder.RenameIndex(
                name: "IX_Tenants_EstateId",
                table: "Tenants",
                newName: "IX_Tenants_AppId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AppId",
                table: "Tenants",
                newName: "EstateId");

            migrationBuilder.RenameIndex(
                name: "IX_Tenants_AppId",
                table: "Tenants",
                newName: "IX_Tenants_EstateId");
        }
    }
}
