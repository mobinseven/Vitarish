using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vitarish.Medical.Infrastructure.Migrations.PrescriptionDb
{
    /// <inheritdoc />
    public partial class TaminOAuthTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "TokenSecretRef",
                schema: "Prescription",
                table: "DoctorProviderCredentials",
                type: "character varying(4096)",
                maxLength: 4096,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(512)",
                oldMaxLength: 512,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SessionId",
                schema: "Prescription",
                table: "DoctorProviderCredentials",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(512)",
                oldMaxLength: 512,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RefreshTokenSecretRef",
                schema: "Prescription",
                table: "DoctorProviderCredentials",
                type: "character varying(4096)",
                maxLength: 4096,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RefreshTokenSecretRef",
                schema: "Prescription",
                table: "DoctorProviderCredentials");

            migrationBuilder.AlterColumn<string>(
                name: "TokenSecretRef",
                schema: "Prescription",
                table: "DoctorProviderCredentials",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(4096)",
                oldMaxLength: 4096,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SessionId",
                schema: "Prescription",
                table: "DoctorProviderCredentials",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2048)",
                oldMaxLength: 2048,
                oldNullable: true);
        }
    }
}
