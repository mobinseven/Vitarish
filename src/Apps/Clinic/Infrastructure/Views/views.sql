/* ===================== mv_Patients ===================== */
CREATE SCHEMA IF NOT EXISTS "Clinic";

DROP MATERIALIZED VIEW IF EXISTS "Clinic"."mv_Patients";

CREATE MATERIALIZED VIEW "Clinic"."mv_Patients" AS
SELECT
  p."Id",
  p."TenantId",
  p."Name",
  p."Description",
  NULLIF(p."Metadata" ->> 'AvatarFileId', '')::uuid AS "AvatarFileId",
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
      SELECT array_agg(pu."UserId" ORDER BY pu."UserId")
      FROM "Sea"."ProjectUsers" pu
      WHERE pu."ProjectId" = p."Id"
        AND pu."DeletedAt" IS NULL
    ),
    ARRAY[]::text[]
  ) AS "UserIds",
  p."Metadata" ->> 'FirstName' AS "FirstName",
  p."Metadata" ->> 'LastName' AS "LastName",
  NULLIF(p."Metadata" ->> 'DateOfBirth', '')::timestamp AS "DateOfBirth",
  NULLIF(p."Metadata" ->> 'DateOfSurgery', '')::timestamp AS "DateOfSurgery",
  COALESCE(NULLIF(p."Metadata" ->> 'Gender', '')::integer, 0) AS "Gender",
  p."Metadata" ->> 'PlaceOfBirth' AS "PlaceOfBirth",
  p."Metadata" ->> 'Address' AS "Address",
  COALESCE(NULLIF(p."Metadata" ->> 'Education', '')::integer, 0) AS "Education",
  p."Metadata" ->> 'Occupation' AS "Occupation",
  p."Metadata" ->> 'FatherName' AS "FatherName",
  COALESCE(NULLIF(p."Metadata" ->> 'Height', '')::integer, 0) AS "Height",
  COALESCE(NULLIF(p."Metadata" ->> 'Weight', '')::integer, 0) AS "Weight",
  p."Metadata" ->> 'Email' AS "Email",
  p."Metadata" ->> 'Phone' AS "Phone",
  p."Metadata" ->> 'Referrer' AS "Referrer",
  p."Metadata" ->> 'InsuranceProvider' AS "InsuranceProvider",
  p."Metadata" ->> 'VisitReason' AS "VisitReason",
  p."Metadata" ->> 'Allergies' AS "Allergies",
  p."Metadata" ->> 'CurrentMedications' AS "CurrentMedications",
  p."Metadata" ->> 'ClinicalNotes' AS "ClinicalNotes",
  p."Metadata" ->> 'EmergencyContactName' AS "EmergencyContactName",
  p."Metadata" ->> 'EmergencyContactPhone' AS "EmergencyContactPhone"
FROM "Sea"."Projects" p
WHERE p."DeletedAt" IS NULL;

CREATE UNIQUE INDEX "IX_mv_Patients_Id"
  ON "Clinic"."mv_Patients" ("Id");
