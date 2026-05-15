using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vitarish.Main.Migrations.ContentsDb
{
    /// <inheritdoc />
    public partial class ContentsInitialization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Assets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    FileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Url = table.Column<string>(type: "nvarchar(2048)", nullable: false),
                    AltText = table.Column<string>(type: "nvarchar(1024)", nullable: true),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValueSql: "'{}'"),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ContentItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(256)", nullable: false),
                    CanonicalPath = table.Column<string>(type: "nvarchar(512)", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(512)", nullable: false),
                    Excerpt = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValueSql: "'[]'"),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValueSql: "'{}'"),
                    AuthorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Locale = table.Column<string>(type: "nvarchar(32)", nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Terms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Slug = table.Column<string>(type: "nvarchar(256)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", nullable: false),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValueSql: "'{}'"),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Terms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Terms_Terms_ParentId",
                        column: x => x.ParentId,
                        principalTable: "Terms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ContentAssets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentAssets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContentAssets_Assets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ContentAssets_ContentItems_ContentId",
                        column: x => x.ContentId,
                        principalTable: "ContentItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContentRelations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromContentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ToContentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RelationType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Weight = table.Column<decimal>(type: "decimal(9,4)", nullable: true),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValueSql: "'{}'"),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentRelations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContentRelations_ContentItems_FromContentId",
                        column: x => x.FromContentId,
                        principalTable: "ContentItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ContentRelations_ContentItems_ToContentId",
                        column: x => x.ToContentId,
                        principalTable: "ContentItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContentRevisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    Snapshot = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentRevisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContentRevisions_ContentItems_ContentId",
                        column: x => x.ContentId,
                        principalTable: "ContentItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContentTerms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TermId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentTerms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContentTerms_ContentItems_ContentId",
                        column: x => x.ContentId,
                        principalTable: "ContentItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ContentTerms_Terms_TermId",
                        column: x => x.TermId,
                        principalTable: "Terms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Assets_FileId",
                table: "Assets",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_Assets_Url",
                table: "Assets",
                column: "Url");

            migrationBuilder.CreateIndex(
                name: "IX_ContentAssets_AssetId",
                table: "ContentAssets",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentAssets_ContentId_AssetId_Role",
                table: "ContentAssets",
                columns: new[] { "ContentId", "AssetId", "Role" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContentAssets_ContentId_Role_Position",
                table: "ContentAssets",
                columns: new[] { "ContentId", "Role", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_ContentItems_AuthorId",
                table: "ContentItems",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentItems_Locale",
                table: "ContentItems",
                column: "Locale");

            migrationBuilder.CreateIndex(
                name: "IX_ContentItems_PublishedAt",
                table: "ContentItems",
                column: "PublishedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ContentItems_TenantId_CanonicalPath",
                table: "ContentItems",
                columns: new[] { "TenantId", "CanonicalPath" },
                unique: true,
                filter: "[CanonicalPath] IS NOT NULL AND [CanonicalPath] != ''");

            migrationBuilder.CreateIndex(
                name: "IX_ContentItems_TenantId_Slug",
                table: "ContentItems",
                columns: new[] { "TenantId", "Slug" },
                unique: true,
                filter: "[Slug] IS NOT NULL AND [Slug] != ''");

            migrationBuilder.CreateIndex(
                name: "IX_ContentItems_Type_Status_PublishedAt",
                table: "ContentItems",
                columns: new[] { "Type", "Status", "PublishedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ContentRelations_FromContentId_ToContentId_RelationType",
                table: "ContentRelations",
                columns: new[] { "FromContentId", "ToContentId", "RelationType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContentRelations_ToContentId_RelationType",
                table: "ContentRelations",
                columns: new[] { "ToContentId", "RelationType" });

            migrationBuilder.CreateIndex(
                name: "IX_ContentRevisions_ContentId_Version",
                table: "ContentRevisions",
                columns: new[] { "ContentId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContentTerms_ContentId_TermId_Role",
                table: "ContentTerms",
                columns: new[] { "ContentId", "TermId", "Role" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContentTerms_TermId_ContentId",
                table: "ContentTerms",
                columns: new[] { "TermId", "ContentId" });

            migrationBuilder.CreateIndex(
                name: "IX_Terms_Kind_ParentId",
                table: "Terms",
                columns: new[] { "Kind", "ParentId" });

            migrationBuilder.CreateIndex(
                name: "IX_Terms_ParentId",
                table: "Terms",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_Terms_TenantId_Slug",
                table: "Terms",
                columns: new[] { "TenantId", "Slug" },
                unique: true,
                filter: "[Slug] IS NOT NULL AND [Slug] != ''");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContentAssets");

            migrationBuilder.DropTable(
                name: "ContentRelations");

            migrationBuilder.DropTable(
                name: "ContentRevisions");

            migrationBuilder.DropTable(
                name: "ContentTerms");

            migrationBuilder.DropTable(
                name: "Assets");

            migrationBuilder.DropTable(
                name: "ContentItems");

            migrationBuilder.DropTable(
                name: "Terms");
        }
    }
}
