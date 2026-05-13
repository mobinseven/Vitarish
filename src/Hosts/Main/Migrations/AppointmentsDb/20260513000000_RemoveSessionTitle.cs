using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vitarish.Main.Migrations.AppointmentsDb
{
    /// <inheritdoc />
    public partial class RemoveSessionTitle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Title",
                table: "Sessions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "Sessions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
