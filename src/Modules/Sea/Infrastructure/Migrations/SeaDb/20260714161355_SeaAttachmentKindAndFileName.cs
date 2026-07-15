using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vitarish.Sea.Infrastructure.Migrations.SeaDb
{
    /// <inheritdoc />
    public partial class SeaAttachmentKindAndFileName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FileName",
                schema: "Sea",
                table: "Attachments",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Kind",
                schema: "Sea",
                table: "Attachments",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FileName",
                schema: "Sea",
                table: "Attachments");

            migrationBuilder.DropColumn(
                name: "Kind",
                schema: "Sea",
                table: "Attachments");
        }
    }
}
