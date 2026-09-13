using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vitarish.Files.Infrastructure.Migrations.FilesDb
{
    /// <inheritdoc />
    public partial class FileIntegrityRecordLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddForeignKey(
                name: "FK_FileIntegrityRecords_Files_FileInformationId",
                schema: "Files",
                table: "FileIntegrityRecords",
                column: "FileInformationId",
                principalSchema: "Files",
                principalTable: "Files",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FileIntegrityRecords_Files_FileInformationId",
                schema: "Files",
                table: "FileIntegrityRecords");
        }
    }
}
