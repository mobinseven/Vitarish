using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vitarish.Medical.Infrastructure.Migrations.PrescriptionDb
{
    /// <inheritdoc />
    public partial class TaminDoctorMobile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProviderDoctorMobile",
                schema: "Prescription",
                table: "DoctorProviderCredentials",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProviderDoctorMobile",
                schema: "Prescription",
                table: "DoctorProviderCredentials");
        }
    }
}
