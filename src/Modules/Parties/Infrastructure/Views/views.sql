BEGIN;

DROP VIEW IF EXISTS "Parties"."vw_UserTenant";

DROP MATERIALIZED VIEW IF EXISTS "Parties"."mv_UserTenant";

CREATE MATERIALIZED VIEW "Parties"."mv_UserTenant" AS
SELECT DISTINCT
  ur."UserId"     AS "UserId",
  t."Id"          AS "TenantId",
  t."Name"        AS "TenantName",
  t."LogoUrl"     AS "TenantLogoUrl",
  t."Description" AS "TenantDescription",
  t."AppId"       AS "TenantAppId",
  ti."Id"         AS "IndicatorId",
  ti."Value"      AS "IndicatorValue"
FROM "Users"."AspNetUserRoles" ur
JOIN "Users"."AspNetRoles" r
  ON r."Id" = ur."RoleId"

CROSS JOIN LATERAL (
  SELECT
    CASE
      WHEN POSITION(':' IN r."Name") > 0
      THEN SUBSTRING(r."Name", 1, POSITION(':' IN r."Name") - 1)
      ELSE NULL
    END AS tenant_id_text
) role_prefix

CROSS JOIN LATERAL (
  SELECT
    CASE
      WHEN role_prefix.tenant_id_text IS NOT NULL
       AND LENGTH(role_prefix.tenant_id_text) = 36
       AND role_prefix.tenant_id_text LIKE '________-____-____-____-____________'
       AND role_prefix.tenant_id_text ~ '^[0-9a-fA-F-]+$'
      THEN role_prefix.tenant_id_text::uuid
      ELSE NULL
    END AS tenant_id
) parsed

JOIN "Parties"."Tenants" t
  ON t."Id" = parsed.tenant_id
 AND t."DeletedAt" IS NULL

LEFT JOIN "Parties"."TenantIndicators" ti
  ON ti."TenantId" = t."Id"
 AND ti."IsPrimary" = TRUE
 AND ti."DeletedAt" IS NULL

WHERE parsed.tenant_id IS NOT NULL

WITH DATA;

CREATE INDEX "IX_mv_UserTenant_UserId"
ON "Parties"."mv_UserTenant" ("UserId");

CREATE INDEX "IX_mv_UserTenant_TenantId"
ON "Parties"."mv_UserTenant" ("TenantId");

CREATE INDEX "IX_mv_UserTenant_UserId_TenantId"
ON "Parties"."mv_UserTenant" ("UserId", "TenantId");

CREATE OR REPLACE VIEW "Parties"."vw_UserTenant" AS
SELECT
  "TenantId",
  "TenantName",
  "TenantLogoUrl",
  "TenantDescription",
  "TenantAppId",
  "IndicatorId",
  "IndicatorValue"
FROM "Parties"."mv_UserTenant"
WHERE "UserId" = current_setting('app.user_id', true);

COMMIT;
