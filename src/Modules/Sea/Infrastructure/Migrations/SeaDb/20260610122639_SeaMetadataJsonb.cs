using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vitarish.Sea.Infrastructure.Migrations.SeaDb
{
    /// <inheritdoc />
    public partial class SeaMetadataJsonb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                            ALTER TABLE "Sea"."Projects"
                                ALTER COLUMN "Metadata" DROP DEFAULT,
                                ALTER COLUMN "Metadata" TYPE jsonb USING "Metadata"::jsonb,
                                ALTER COLUMN "Metadata" SET DEFAULT '{}'::jsonb;
                            """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                            ALTER TABLE "Sea"."Projects"
                                ALTER COLUMN "Metadata" DROP DEFAULT,
                                ALTER COLUMN "Metadata" TYPE text USING "Metadata"::text,
                                ALTER COLUMN "Metadata" SET DEFAULT '{}';
                            """);
        }
    }
}
