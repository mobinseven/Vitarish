using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vitarish.Main.Migrations.PrescriptionDb
{
    /// <inheritdoc />
    public partial class AddProviderClinicIdentifierToDoctorCredential : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProviderClinicIdentifier",
                schema: "Prescription",
                table: "DoctorProviderCredentials",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProviderClinicIdentifier",
                schema: "Prescription",
                table: "DoctorProviderCredentials");
        }
    }
}
