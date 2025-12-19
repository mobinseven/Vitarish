using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vitarish.Host.Migrations.ApiClientDb
{
    /// <inheritdoc />
    public partial class RemoveTenantOwnerClaims : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "ApiClients");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "ApiClients");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "ApiClients",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OwnerId",
                table: "ApiClients",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
