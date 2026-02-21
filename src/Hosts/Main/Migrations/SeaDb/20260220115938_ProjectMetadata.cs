using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vitarish.Main.Migrations.SeaDb
{
    /// <inheritdoc />
    public partial class ProjectMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // First, update any existing NULL values to the default '{}'
            migrationBuilder.Sql(
                @"UPDATE Projects
                   SET Metadata = N'{}'
                WHERE Metadata IS NULL 
                   OR TRIM(Metadata) = N''
                   OR Metadata = N'null'
                   OR Metadata = N'""';"
            );
            migrationBuilder.AlterColumn<string>(
                name: "Metadata",
                table: "Projects",
                type: "nvarchar(max)",
                nullable: false,
                defaultValueSql: "'{}'",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Metadata",
                table: "Projects",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldDefaultValueSql: "'{}'");
        }
    }
}
