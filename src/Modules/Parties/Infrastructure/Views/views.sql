-- Parties module SQL views
-- Cross-schema view uses current_setting('app.user_id', true) set via RequestContextInterceptor

/* ===================== vw_UserTenant ===================== */
-- Cross-schema view to retrieve current user's tenants with their primary indicators
-- Retrieves tenants based on the current user's roles (app.user_id) by parsing tenant IDs from role names
CREATE OR REPLACE VIEW "Parties"."vw_UserTenant" AS
SELECT DISTINCT
  t."Id"          AS "TenantId",
  t."Name"        AS "TenantName",
  t."LogoUrl"     AS "TenantLogoUrl",
  t."Description" AS "TenantDescription",
  t."AppId"       AS "TenantAppId",
  ti."Id"         AS "IndicatorId",
  ti."Value"      AS "IndicatorValue"
FROM "Users"."AspNetUserRoles" ur
JOIN "Users"."AspNetRoles" r ON r."Id" = ur."RoleId"
CROSS JOIN LATERAL (
  SELECT 
    CASE 
      WHEN POSITION(':' IN r."Name") > 0 
      THEN SUBSTRING(r."Name", 1, POSITION(':' IN r."Name") - 1)
      ELSE NULL
    END AS tenant_id_text
) parsed
JOIN "Parties"."Tenants" t 
  ON t."Id" = parsed.tenant_id_text::uuid
  AND t."DeletedAt" IS NULL
LEFT JOIN "Parties"."TenantIndicators" ti 
  ON ti."TenantId" = t."Id" 
  AND ti."IsPrimary" = TRUE
  AND ti."DeletedAt" IS NULL
WHERE ur."UserId" = current_setting('app.user_id', true)
  AND POSITION(':' IN r."Name") > 0
  AND parsed.tenant_id_text IS NOT NULL;
