CREATE SCHEMA IF NOT EXISTS "Clinic";

DROP MATERIALIZED VIEW IF EXISTS "Clinic"."mv_Patients" CASCADE;

CREATE MATERIALIZED VIEW "Clinic"."mv_Patients" AS
WITH src AS (
  SELECT
    p.*,
    COALESCE(p."Metadata"::jsonb, '{{}}'::jsonb) AS md
  FROM "Sea"."Projects" p
  WHERE p."DeletedAt" IS NULL
)
SELECT
  p."Id",
  p."TenantId",
  p."Name",
  p."Description",

  NULLIF(p.md ->> 'AvatarFileId', '')::uuid AS "AvatarFileId",

  p."IdentificationCode",
  p."Template",
  p."CreatedAt",
  p."CreatedBy",
  p."UpdatedAt",
  p."UpdatedBy",
  p."DeletedAt",
  p."DeletedBy",

  COALESCE(
    (
      SELECT array_agg(pu."UserId"::text ORDER BY pu."UserId")
      FROM "Sea"."ProjectUsers" pu
      WHERE pu."ProjectId" = p."Id"
        AND pu."DeletedAt" IS NULL
    ),
    ARRAY[]::text[]
  ) AS "UserIds",

  p.md ->> 'FirstName' AS "FirstName",
  p.md ->> 'LastName' AS "LastName",

  NULLIF(p.md ->> 'DateOfBirth', '')::timestamptz AS "DateOfBirth",
  NULLIF(p.md ->> 'DateOfSurgery', '')::timestamptz AS "DateOfSurgery",

  COALESCE(NULLIF(p.md ->> 'Gender', '')::integer, 0) AS "Gender",

  p.md ->> 'PlaceOfBirth' AS "PlaceOfBirth",
  p.md ->> 'Address' AS "Address",

  COALESCE(NULLIF(p.md ->> 'Education', '')::integer, 0) AS "Education",

  p.md ->> 'Occupation' AS "Occupation",
  p.md ->> 'FatherName' AS "FatherName",

  COALESCE(NULLIF(p.md ->> 'Height', '')::integer, 0) AS "Height",
  COALESCE(NULLIF(p.md ->> 'Weight', '')::integer, 0) AS "Weight",

  p.md ->> 'Email' AS "Email",
  p.md ->> 'Phone' AS "Phone",
  p.md ->> 'Referrer' AS "Referrer",
  p.md ->> 'InsuranceProvider' AS "InsuranceProvider",
  p.md ->> 'VisitReason' AS "VisitReason",
  p.md ->> 'Allergies' AS "Allergies",
  p.md ->> 'CurrentMedications' AS "CurrentMedications",
  p.md ->> 'ClinicalNotes' AS "ClinicalNotes",
  p.md ->> 'EmergencyContactName' AS "EmergencyContactName",
  p.md ->> 'EmergencyContactPhone' AS "EmergencyContactPhone"

FROM src p;

CREATE UNIQUE INDEX "IX_mv_Patients_Id"
  ON "Clinic"."mv_Patients" ("Id");
