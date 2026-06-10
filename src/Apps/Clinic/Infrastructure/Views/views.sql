/* ===================== mv_Patients ===================== */
CREATE SCHEMA IF NOT EXISTS "Clinic";

DROP MATERIALIZED VIEW IF EXISTS "Clinic"."mv_Patients";

CREATE MATERIALIZED VIEW "Clinic"."mv_Patients" AS
SELECT
  p."Id",
  p."TenantId",
  p."Name",
  p."Description",
  p."IdentificationCode",
  p."Template",
  p."CreatedAt",
  p."CreatedBy",
  p."UpdatedAt",
  p."UpdatedBy",
  p."Metadata" ->> 'FirstName' AS "FirstName",
  p."Metadata" ->> 'LastName' AS "LastName",
  p."Metadata" ->> 'DateOfBirth' AS "DateOfBirth",
  p."Metadata" ->> 'Phone' AS "Phone",
  p."Metadata" ->> 'Email' AS "Email"
FROM "Sea"."Projects" p
WHERE p."DeletedAt" IS NULL;

CREATE UNIQUE INDEX "IX_mv_Patients_Id"
  ON "Clinic"."mv_Patients" ("Id");
