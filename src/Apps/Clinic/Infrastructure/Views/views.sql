/* ===================== mv_Patients ===================== */
DROP MATERIALIZED VIEW IF EXISTS "Sea"."mv_Patients";

CREATE MATERIALIZED VIEW "Sea"."mv_Patients" AS
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
  ON "Sea"."mv_Patients" ("Id");
