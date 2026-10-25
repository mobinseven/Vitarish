using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vitarish.Authentication.Infrastructure.Migrations.AuthenticationDb
{
    /// <inheritdoc />
    public partial class AuthenticationAccessHistoryIpAddresses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LastSuccessfulIpAddress",
                schema: "Authentication",
                table: "AuthenticationAccessHistories",
                type: "character varying(45)",
                maxLength: 45,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastUnsuccessfulIpAddress",
                schema: "Authentication",
                table: "AuthenticationAccessHistories",
                type: "character varying(45)",
                maxLength: 45,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreviousSuccessfulIpAddress",
                schema: "Authentication",
                table: "AuthenticationAccessHistories",
                type: "character varying(45)",
                maxLength: 45,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastSuccessfulIpAddress",
                schema: "Authentication",
                table: "AuthenticationAccessHistories");

            migrationBuilder.DropColumn(
                name: "LastUnsuccessfulIpAddress",
                schema: "Authentication",
                table: "AuthenticationAccessHistories");

            migrationBuilder.DropColumn(
                name: "PreviousSuccessfulIpAddress",
                schema: "Authentication",
                table: "AuthenticationAccessHistories");
        }
    }
}
