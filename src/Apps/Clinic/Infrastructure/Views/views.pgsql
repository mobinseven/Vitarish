CREATE SCHEMA IF NOT EXISTS "Clinic";

CREATE OR REPLACE FUNCTION "Clinic"."try_int"(value text)
RETURNS integer
LANGUAGE plpgsql
IMMUTABLE
PARALLEL SAFE
AS $$
DECLARE
  v text := NULLIF(btrim(value), '');
BEGIN
  IF v IS NULL THEN
    RETURN NULL;
  END IF;

  RETURN v::integer;

EXCEPTION
  WHEN invalid_text_representation OR numeric_value_out_of_range THEN
    RETURN NULL;
END;
$$;

CREATE OR REPLACE FUNCTION "Clinic"."try_uuid"(value text)
RETURNS uuid
LANGUAGE plpgsql
IMMUTABLE
PARALLEL SAFE
AS $$
DECLARE
  v text := NULLIF(btrim(value), '');
BEGIN
  IF v IS NULL THEN
    RETURN NULL;
  END IF;

  RETURN v::uuid;

EXCEPTION
  WHEN invalid_text_representation THEN
    RETURN NULL;
END;
$$;

CREATE OR REPLACE FUNCTION "Clinic"."try_timestamptz"(value text)
RETURNS timestamptz
LANGUAGE plpgsql
STABLE
PARALLEL SAFE
AS $$
DECLARE
  v text := NULLIF(btrim(value), '');
BEGIN
  IF v IS NULL THEN
    RETURN NULL;
  END IF;

  RETURN v::timestamptz;

EXCEPTION
  WHEN invalid_datetime_format
    OR datetime_field_overflow
    OR invalid_text_representation THEN
    RETURN NULL;
END;
$$;

DROP MATERIALIZED VIEW IF EXISTS "Clinic"."mv_Patients" CASCADE;

CREATE MATERIALIZED VIEW "Clinic"."mv_Patients" AS
WITH src AS (
  SELECT
    p.*,
    COALESCE(p."Metadata"::jsonb, jsonb_build_object()) AS md
  FROM "Sea"."Projects" p
  WHERE p."DeletedAt" IS NULL
)
SELECT
  p."Id",
  p."TenantId",
  p."Name",
  p."Description",

  "Clinic"."try_uuid"(p.md ->> 'AvatarFileId') AS "AvatarFileId",

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

  CASE
    WHEN jsonb_typeof(p.md -> 'Tags') = 'array' THEN COALESCE(
      ARRAY(
        SELECT tag
        FROM (
          SELECT DISTINCT ON (lower(btrim(value)))
            btrim(value) AS tag,
            position
          FROM jsonb_array_elements_text(p.md -> 'Tags') WITH ORDINALITY AS value(value, position)
          WHERE btrim(value) <> ''
          ORDER BY lower(btrim(value)), position
        ) normalized
        ORDER BY position
      ),
      ARRAY[]::text[]
    )
    ELSE ARRAY[]::text[]
  END AS "Tags",

  p.md ->> 'FirstName' AS "FirstName",
  p.md ->> 'LastName' AS "LastName",

  "Clinic"."try_timestamptz"(p.md ->> 'DateOfBirth') AS "DateOfBirth",

  COALESCE("Clinic"."try_int"(p.md ->> 'Gender'), 0) AS "Gender",

  p.md ->> 'PlaceOfBirth' AS "PlaceOfBirth",
  p.md ->> 'Address' AS "Address",

  COALESCE("Clinic"."try_int"(p.md ->> 'Education'), 0) AS "Education",

  p.md ->> 'Occupation' AS "Occupation",
  p.md ->> 'FatherName' AS "FatherName",

  COALESCE("Clinic"."try_int"(p.md ->> 'Height'), 0) AS "Height",
  COALESCE("Clinic"."try_int"(p.md ->> 'Weight'), 0) AS "Weight",

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

CREATE INDEX "IX_mv_Patients_Tags"
  ON "Clinic"."mv_Patients" USING GIN ("Tags");

DROP MATERIALIZED VIEW IF EXISTS "Clinic"."mv_SurgerySuggestions";

DROP MATERIALIZED VIEW IF EXISTS "Clinic"."mv_PatientSurgeries";

CREATE MATERIALIZED VIEW "Clinic"."mv_PatientSurgeries" AS
SELECT
  po."Id",
  po."TenantId",
  po."OwnerEntityId" AS "PatientId",
  po."ObjectType",
  po."ConcurrencyToken",
  po."Data",
  po."Data" ->> 'CatalogId' AS "CatalogId",
  po."Data" ->> 'Name' AS "Name",
  po."Data" ->> 'Status' AS "Status",
  (po."Data" ->> 'ScheduledAt')::timestamptz AS "ScheduledAt",
  po."Data" -> 'Tools' AS "Tools",
  po."Data" -> 'PersonsToNotify' AS "PersonsToNotify",
  po."Data" -> 'Contacts' AS "Contacts",
  po."Data" -> 'ReminderPolicies' AS "ReminderPolicies",
  "Clinic"."try_int"(po."Data" ->> 'DurationMinutes') AS "DurationMinutes",
  NULLIF(btrim(po."Data" ->> 'Location'), '') AS "Location",
  COALESCE(po."Data" -> 'Team', '[]'::jsonb) AS "Team",
  po."CreatedAt",
  po."UpdatedAt"
FROM "Sea"."ProjectObjects" po
WHERE po."OwnerEntityType" = 'patient'
  AND po."ObjectType" = 'patient.surgery'
  AND po."DeletedAt" IS NULL;

CREATE UNIQUE INDEX "IX_mv_PatientSurgeries_Id"
  ON "Clinic"."mv_PatientSurgeries" ("Id");

CREATE INDEX "IX_mv_PatientSurgeries_TenantId_PatientId"
  ON "Clinic"."mv_PatientSurgeries" ("TenantId", "PatientId");

CREATE INDEX "IX_mv_PatientSurgeries_TenantId_ScheduledAt"
  ON "Clinic"."mv_PatientSurgeries" ("TenantId", "ScheduledAt");

CREATE MATERIALIZED VIEW "Clinic"."mv_SurgerySuggestions" AS
SELECT
  s."TenantId" AS "TenantId",
  'location' AS "Kind",
  s."Location" AS "Value",
  count(*)::bigint AS "UseCount",
  max(GREATEST(s."CreatedAt", COALESCE(s."UpdatedAt", s."CreatedAt"))) AS "LastUsedAt"
FROM "Clinic"."mv_PatientSurgeries" s
WHERE s."Location" IS NOT NULL
GROUP BY s."TenantId", s."Location"

UNION ALL

SELECT
  s."TenantId" AS "TenantId",
  'role' AS "Kind",
  btrim(member ->> 'Role') AS "Value",
  count(*)::bigint AS "UseCount",
  max(GREATEST(s."CreatedAt", COALESCE(s."UpdatedAt", s."CreatedAt"))) AS "LastUsedAt"
FROM "Clinic"."mv_PatientSurgeries" s
CROSS JOIN LATERAL jsonb_array_elements(s."Team") AS member
WHERE btrim(member ->> 'Role') <> ''
GROUP BY s."TenantId", btrim(member ->> 'Role');

CREATE UNIQUE INDEX "IX_mv_SurgerySuggestions_TenantId_Kind_Value"
  ON "Clinic"."mv_SurgerySuggestions" ("TenantId", "Kind", "Value");
