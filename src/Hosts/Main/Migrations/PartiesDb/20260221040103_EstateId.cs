using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vitarish.Host.Migrations.PartiesDb
{
    /// <inheritdoc />
    public partial class EstateId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "EstateId",
                table: "Tenants",
                newName: "EstateId");

            migrationBuilder.RenameIndex(
                name: "IX_Tenants_ClientId",
                table: "Tenants",
                newName: "IX_Tenants_EstateId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "EstateId",
                table: "Tenants",
                newName: "EstateId");

            migrationBuilder.RenameIndex(
                name: "IX_Tenants_EstateId",
                table: "Tenants",
                newName: "IX_Tenants_ClientId");
        }
    }
}
