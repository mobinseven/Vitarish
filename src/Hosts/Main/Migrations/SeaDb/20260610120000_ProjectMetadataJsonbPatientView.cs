using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vitarish.Main.Migrations.SeaDb
{
    /// <inheritdoc />
    public partial class ProjectMetadataJsonbPatientView : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION pg_temp.is_valid_jsonb(value text)
                RETURNS boolean
                LANGUAGE plpgsql
                AS $function$
                BEGIN
                    PERFORM value::jsonb;
                    RETURN true;
                EXCEPTION WHEN others THEN
                    RETURN false;
                END;
                $function$;

                DO $block$
                DECLARE
                    invalid_project_id uuid;
                BEGIN
                    SELECT "Id"
                    INTO invalid_project_id
                    FROM "Sea"."Projects"
                    WHERE "Metadata" IS NOT NULL
                      AND NOT pg_temp.is_valid_jsonb("Metadata")
                    LIMIT 1;

                    IF invalid_project_id IS NOT NULL THEN
                        RAISE EXCEPTION 'Sea.Projects.Metadata contains invalid JSON for project %. Correct the value before applying ProjectMetadataJsonbPatientView.', invalid_project_id;
                    END IF;
                END;
                $block$;
                """);

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
