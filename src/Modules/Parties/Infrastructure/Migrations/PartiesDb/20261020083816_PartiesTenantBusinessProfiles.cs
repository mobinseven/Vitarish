using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vitarish.Parties.Infrastructure.Migrations.PartiesDb
{
    /// <inheritdoc />
    public partial class PartiesTenantBusinessProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TenantBusinessProfiles",
                schema: "Parties",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Details = table.Column<string>(type: "jsonb", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantBusinessProfiles", x => x.TenantId);
                    table.ForeignKey(
                        name: "FK_TenantBusinessProfiles_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "Parties",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TenantBusinessProfiles",
                schema: "Parties");
        }
    }
}
