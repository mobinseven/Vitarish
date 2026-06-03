-- Parties module SQL views

/* ===================== vw_UserTenants ===================== */
-- View to retrieve user's tenants with their primary indicators in a single query
CREATE OR REPLACE VIEW "Parties"."vw_UserTenants" AS
SELECT
  t."Id"          AS "TenantId",
  t."Name"        AS "TenantName",
  t."LogoUrl"     AS "TenantLogoUrl",
  t."Description" AS "TenantDescription",
  t."AppId"       AS "TenantAppId",
  t."Metadata"    AS "Metadata",
  ti."Id"         AS "IndicatorId",
  ti."Value"      AS "IndicatorValue"
FROM "Parties"."Tenants" t
LEFT JOIN "Parties"."TenantIndicators" ti 
  ON ti."TenantId" = t."Id" 
  AND ti."IsPrimary" = TRUE
  AND ti."DeletedAt" IS NULL
WHERE t."DeletedAt" IS NULL;
