using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vitarish.Authentication.Infrastructure.Migrations.AuthenticationDb
{
    /// <inheritdoc />
    public partial class AuthenticationLongSessionIdleExpiryDefault : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "DefaultInactivityTimeoutMinutes",
                schema: "Authentication",
                table: "SecurityAccessPolicies",
                type: "integer",
                nullable: false,
                defaultValue: 20160,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 30);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Builds before this migration accept idle expiries of at most 30 minutes.
            migrationBuilder.Sql("""
                UPDATE "Authentication"."SecurityAccessPolicies"
                SET "DefaultInactivityTimeoutMinutes" = 30, "Version" = "Version" + 1
                WHERE "DefaultInactivityTimeoutMinutes" > 30;
                UPDATE "Authentication"."AuthenticationUserSessionPolicies"
                SET "InactivityTimeoutMinutes" = 30, "Version" = "Version" + 1
                WHERE "InactivityTimeoutMinutes" > 30;
                """);

            migrationBuilder.AlterColumn<int>(
                name: "DefaultInactivityTimeoutMinutes",
                schema: "Authentication",
                table: "SecurityAccessPolicies",
                type: "integer",
                nullable: false,
                defaultValue: 30,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 20160);
        }
    }
}
