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
