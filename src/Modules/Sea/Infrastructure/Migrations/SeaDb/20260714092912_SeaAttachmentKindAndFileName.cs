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
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE "Sea"."Attachments"
                SET "Kind" = CASE
                    WHEN "Content" ~* '^data:' THEN 1
                    WHEN "Content" ~* '^[a-z][a-z0-9+.-]*://' THEN 1
                    WHEN "Content" ~* '^\\{?[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}\\}?$' THEN 1
                    ELSE 0
                END
                WHERE "Kind" IS NULL;
                """);

            migrationBuilder.AlterColumn<int>(
                name: "Kind",
                schema: "Sea",
                table: "Attachments",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Kind",
                schema: "Sea",
                table: "Attachments");

            migrationBuilder.DropColumn(
                name: "FileName",
                schema: "Sea",
                table: "Attachments");
        }
    }
}
