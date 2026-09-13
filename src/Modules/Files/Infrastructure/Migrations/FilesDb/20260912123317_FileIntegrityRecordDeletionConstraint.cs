using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vitarish.Files.Infrastructure.Migrations.FilesDb
{
    /// <inheritdoc />
    public partial class FileIntegrityRecordDeletionConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FileIntegrityRecords_Files_FileInformationId",
                schema: "Files",
                table: "FileIntegrityRecords");

            migrationBuilder.AddForeignKey(
                name: "FK_FileIntegrityRecords_Files_FileInformationId",
                schema: "Files",
                table: "FileIntegrityRecords",
                column: "FileInformationId",
                principalSchema: "Files",
                principalTable: "Files",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FileIntegrityRecords_Files_FileInformationId",
                schema: "Files",
                table: "FileIntegrityRecords");

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
    }
}
