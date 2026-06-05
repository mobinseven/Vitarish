using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Vitarish.Platform.Infrastructure.Data;

#nullable disable

namespace Vitarish.Main.Migrations.PlatformDb
{
    /// <inheritdoc />
    [DbContext(typeof(PlatformDbContext))]
    [Migration("20260605120000_PlatformThemeInitialization")]
    public partial class PlatformThemeInitialization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(name: "Platform");

            migrationBuilder.CreateTable(
                name: "Themes",
                schema: "Platform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    LightPrimary = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    LightPrimaryContrastText = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    LightSecondary = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    LightSecondaryContrastText = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    LightBackground = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    LightSurface = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    LightAppbarBackground = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    LightAppbarText = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    LightTextPrimary = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    LightTextSecondary = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    LightDivider = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    DarkPrimary = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    DarkPrimaryContrastText = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    DarkSecondary = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    DarkSecondaryContrastText = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    DarkBackground = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    DarkSurface = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    DarkAppbarBackground = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    DarkAppbarText = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    DarkTextPrimary = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    DarkTextSecondary = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    DarkDivider = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_Themes", x => x.Id));

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

            migrationBuilder.InsertData(
                schema: "Platform",
                table: "Themes",
                columns: new[] { "Id", "Name", "LightPrimary", "LightPrimaryContrastText", "LightSecondary", "LightSecondaryContrastText", "LightBackground", "LightSurface", "LightAppbarBackground", "LightAppbarText", "LightTextPrimary", "LightTextSecondary", "LightDivider", "DarkPrimary", "DarkPrimaryContrastText", "DarkSecondary", "DarkSecondaryContrastText", "DarkBackground", "DarkSurface", "DarkAppbarBackground", "DarkAppbarText", "DarkTextPrimary", "DarkTextSecondary", "DarkDivider", "CreatedAt", "UpdatedAt" },
                values: new object[] { new Guid("3af03f4e-e53a-4f63-97cc-820c5ea52d85"), "Default", "#2563EB", "#FFFFFF", "#0EA5E9", "#001018", "#F1F1F1", "#F1F1F1", "#FFFFFF", "#0F172A", "#0F172A", "#334155", "#E2E8F0", "#60A5FA", "#07101F", "#38BDF8", "#041018", "#323232", "#323232", "#434343", "#E2E8F0", "#E2E8F0", "#94A3B8", "#233044", new DateTimeOffset(new DateTime(2026, 6, 5, 12, 0, 0, DateTimeKind.Utc)), new DateTimeOffset(new DateTime(2026, 6, 5, 12, 0, 0, DateTimeKind.Utc)) });

            migrationBuilder.InsertData(
                schema: "Platform",
                table: "ThemeSelections",
                columns: new[] { "Id", "ActiveThemeId", "UpdatedAt" },
                values: new object[] { 1, new Guid("3af03f4e-e53a-4f63-97cc-820c5ea52d85"), new DateTimeOffset(new DateTime(2026, 6, 5, 12, 0, 0, DateTimeKind.Utc)) });

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
            migrationBuilder.DropTable(name: "ThemeSelections", schema: "Platform");
            migrationBuilder.DropTable(name: "Themes", schema: "Platform");
        }
    }
}
