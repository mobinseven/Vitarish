using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vitarish.Knowledge.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class KnowledgeInitialization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Knowledge");

            migrationBuilder.CreateTable(
                name: "KnowledgeCorpora",
                schema: "Knowledge",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Description = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeCorpora", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KnowledgeAskTraces",
                schema: "Knowledge",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CorpusId = table.Column<Guid>(type: "uuid", nullable: false),
                    Question = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    RetrievedChunkIdsJson = table.Column<string>(type: "text", nullable: false),
                    GeneratedAnswer = table.Column<string>(type: "text", nullable: false),
                    MissingInformation = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: true),
                    CitationsJson = table.Column<string>(type: "text", nullable: false),
                    TraceKind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    IsAnswered = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeAskTraces", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KnowledgeAskTraces_KnowledgeCorpora_CorpusId",
                        column: x => x.CorpusId,
                        principalSchema: "Knowledge",
                        principalTable: "KnowledgeCorpora",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KnowledgeIndexRuns",
                schema: "Knowledge",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CorpusId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    IndexedChunkCount = table.Column<int>(type: "integer", nullable: false),
                    Scope = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    SourceId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeIndexRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KnowledgeIndexRuns_KnowledgeCorpora_CorpusId",
                        column: x => x.CorpusId,
                        principalSchema: "Knowledge",
                        principalTable: "KnowledgeCorpora",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KnowledgeQuestions",
                schema: "Knowledge",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CorpusId = table.Column<Guid>(type: "uuid", nullable: false),
                    Question = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    ExpectedAnswer = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: true),
                    ExpectedChunkIdsJson = table.Column<Guid[]>(type: "uuid[]", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KnowledgeQuestions_KnowledgeCorpora_CorpusId",
                        column: x => x.CorpusId,
                        principalSchema: "Knowledge",
                        principalTable: "KnowledgeCorpora",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KnowledgeSources",
                schema: "Knowledge",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CorpusId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    SourceType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    OriginalLocation = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    Version = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Checksum = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ImportedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeSources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KnowledgeSources_KnowledgeCorpora_CorpusId",
                        column: x => x.CorpusId,
                        principalSchema: "Knowledge",
                        principalTable: "KnowledgeCorpora",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KnowledgeDocuments",
                schema: "Knowledge",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CorpusId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false),
                    Product = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Version = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Language = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KnowledgeDocuments_KnowledgeSources_SourceId",
                        column: x => x.SourceId,
                        principalSchema: "Knowledge",
                        principalTable: "KnowledgeSources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KnowledgeChunks",
                schema: "Knowledge",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CorpusId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    HeadingPath = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeChunks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KnowledgeChunks_KnowledgeDocuments_DocumentId",
                        column: x => x.DocumentId,
                        principalSchema: "Knowledge",
                        principalTable: "KnowledgeDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeAskTraces_CorpusId",
                schema: "Knowledge",
                table: "KnowledgeAskTraces",
                column: "CorpusId");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeAskTraces_TenantId_CorpusId_CreatedAt",
                schema: "Knowledge",
                table: "KnowledgeAskTraces",
                columns: new[] { "TenantId", "CorpusId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeChunks_CorpusId_SourceId_DocumentId_Order",
                schema: "Knowledge",
                table: "KnowledgeChunks",
                columns: new[] { "CorpusId", "SourceId", "DocumentId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeChunks_DocumentId",
                schema: "Knowledge",
                table: "KnowledgeChunks",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeCorpora_TenantId",
                schema: "Knowledge",
                table: "KnowledgeCorpora",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeDocuments_CorpusId",
                schema: "Knowledge",
                table: "KnowledgeDocuments",
                column: "CorpusId");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeDocuments_SourceId",
                schema: "Knowledge",
                table: "KnowledgeDocuments",
                column: "SourceId");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeIndexRuns_CorpusId",
                schema: "Knowledge",
                table: "KnowledgeIndexRuns",
                column: "CorpusId");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeIndexRuns_TenantId_CorpusId_CreatedAt",
                schema: "Knowledge",
                table: "KnowledgeIndexRuns",
                columns: new[] { "TenantId", "CorpusId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeQuestions_CorpusId",
                schema: "Knowledge",
                table: "KnowledgeQuestions",
                column: "CorpusId");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeSources_CorpusId",
                schema: "Knowledge",
                table: "KnowledgeSources",
                column: "CorpusId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KnowledgeAskTraces",
                schema: "Knowledge");

            migrationBuilder.DropTable(
                name: "KnowledgeChunks",
                schema: "Knowledge");

            migrationBuilder.DropTable(
                name: "KnowledgeIndexRuns",
                schema: "Knowledge");

            migrationBuilder.DropTable(
                name: "KnowledgeQuestions",
                schema: "Knowledge");

            migrationBuilder.DropTable(
                name: "KnowledgeDocuments",
                schema: "Knowledge");

            migrationBuilder.DropTable(
                name: "KnowledgeSources",
                schema: "Knowledge");

            migrationBuilder.DropTable(
                name: "KnowledgeCorpora",
                schema: "Knowledge");
        }
    }
}
