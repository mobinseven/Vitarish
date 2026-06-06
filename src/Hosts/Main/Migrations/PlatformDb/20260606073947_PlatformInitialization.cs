using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vitarish.Main.Migrations.PlatformDb
{
    /// <inheritdoc />
    public partial class PlatformInitialization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Platform");

            migrationBuilder.CreateTable(
                name: "Themes",
                schema: "Platform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    DarkPalette = table.Column<string>(type: "jsonb", nullable: false),
                    LightPalette = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Themes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ThemeSelections",
                schema: "Platform",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    ActiveThemeId = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThemeSelections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ThemeSelections_Themes_ActiveThemeId",
                        column: x => x.ActiveThemeId,
                        principalSchema: "Platform",
                        principalTable: "Themes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Themes_Name",
                schema: "Platform",
                table: "Themes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ThemeSelections_ActiveThemeId",
                schema: "Platform",
                table: "ThemeSelections",
                column: "ActiveThemeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ThemeSelections",
                schema: "Platform");

            migrationBuilder.DropTable(
                name: "Themes",
                schema: "Platform");
        }
    }
}
