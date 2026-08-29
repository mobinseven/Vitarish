using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vitarish.Authentication.Infrastructure.Migrations.AuthenticationDb
{
    /// <inheritdoc />
    public partial class AuthenticationSecurityAccessPolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Authentication");

            migrationBuilder.CreateTable(
                name: "SecurityAccessPolicies",
                schema: "Authentication",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    MinimumPasswordLength = table.Column<int>(type: "integer", nullable: false),
                    RequirePasswordLowercase = table.Column<bool>(type: "boolean", nullable: false),
                    RequirePasswordUppercase = table.Column<bool>(type: "boolean", nullable: false),
                    RequirePasswordNumber = table.Column<bool>(type: "boolean", nullable: false),
                    RequirePasswordSpecialCharacter = table.Column<bool>(type: "boolean", nullable: false),
                    FailedAttemptThreshold = table.Column<int>(type: "integer", nullable: false),
                    FailedAttemptDenialDurationSeconds = table.Column<int>(type: "integer", nullable: false),
                    FailedAttemptResponse = table.Column<int>(type: "integer", nullable: false),
                    EnabledAuthenticationMechanisms = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    PermittedPreAuthenticationActions = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecurityAccessPolicies", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SecurityAccessPolicies",
                schema: "Authentication");
        }
    }
}
