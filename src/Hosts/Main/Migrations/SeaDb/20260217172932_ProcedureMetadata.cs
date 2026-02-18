using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vitarish.Main.Migrations.SeaDb
{
    /// <inheritdoc />
    public partial class ProcedureMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // First, update any existing NULL values to the default '{}'
            migrationBuilder.Sql(
                @"UPDATE Procedures
                  SET Metadata = N'{}'"
            );

            // Then, alter the column to make it non-nullable with the default
            migrationBuilder.AlterColumn<string>(
                name: "Metadata",
                table: "Procedures",
                type: "nvarchar(max)",
                nullable: false,
                defaultValueSql: "'{}'",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Metadata",
                table: "Procedures",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldDefaultValueSql: "'{}'");
        }
    }
}
