using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vitarish.Parties.Infrastructure.Migrations.PartiesDb
{
    /// <inheritdoc />
    public partial class PartiesTenantLogoPromptDismissal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LogoPromptDismissedAt",
                schema: "Parties",
                table: "TenantBusinessProfiles",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LogoPromptDismissedAt",
                schema: "Parties",
                table: "TenantBusinessProfiles");
        }
    }
}
