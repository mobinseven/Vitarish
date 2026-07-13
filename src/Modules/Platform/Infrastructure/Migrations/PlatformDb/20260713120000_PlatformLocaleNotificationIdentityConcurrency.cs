using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vitarish.Platform.Infrastructure.Migrations.PlatformDb
{
    /// <inheritdoc />
    public partial class PlatformLocaleNotificationIdentityConcurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "Id",
                schema: "Platform",
                table: "LocaleNotificationRecords",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                schema: "Platform",
                table: "LocaleNotificationRecords",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                schema: "Platform",
                table: "LocaleNotificationRecords",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()");

            migrationBuilder.AddColumn<long>(
                name: "Version",
                schema: "Platform",
                table: "LocaleNotificationRecords",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.DropPrimaryKey(
                name: "PK_LocaleNotificationRecords",
                schema: "Platform",
                table: "LocaleNotificationRecords");

            migrationBuilder.AddPrimaryKey(
                name: "PK_LocaleNotificationRecords",
                schema: "Platform",
                table: "LocaleNotificationRecords",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_LocaleNotificationRecords_NormalizedLocale",
                schema: "Platform",
                table: "LocaleNotificationRecords",
                column: "NormalizedLocale",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LocaleNotificationRecords_NormalizedLocale",
                schema: "Platform",
                table: "LocaleNotificationRecords");

            migrationBuilder.DropPrimaryKey(
                name: "PK_LocaleNotificationRecords",
                schema: "Platform",
                table: "LocaleNotificationRecords");

            migrationBuilder.AddPrimaryKey(
                name: "PK_LocaleNotificationRecords",
                schema: "Platform",
                table: "LocaleNotificationRecords",
                column: "NormalizedLocale");

            migrationBuilder.DropColumn(name: "Id", schema: "Platform", table: "LocaleNotificationRecords");
            migrationBuilder.DropColumn(name: "CreatedAt", schema: "Platform", table: "LocaleNotificationRecords");
            migrationBuilder.DropColumn(name: "UpdatedAt", schema: "Platform", table: "LocaleNotificationRecords");
            migrationBuilder.DropColumn(name: "Version", schema: "Platform", table: "LocaleNotificationRecords");
        }
    }
}
