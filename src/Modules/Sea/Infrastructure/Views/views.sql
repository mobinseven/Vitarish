-- Sea module SQL views
-- Row-level security uses current_setting('app.user_id', true) set via RequestContextInterceptor

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

/* ===================== vw_ProjectStats ===================== */
CREATE OR REPLACE VIEW "Sea"."vw_ProjectStats" AS
SELECT
  p."Id" AS "ProjectId",

  (SELECT COUNT(*) FROM "Sea"."Procedures" pr WHERE pr."ProjectId" = p."Id" AND pr."DeletedAt" IS NULL) AS "Procedures",
  (SELECT COUNT(*) FROM "Sea"."Events" e WHERE e."ProjectId" = p."Id" AND e."DeletedAt" IS NULL) AS "Events",
  (SELECT COUNT(*) FROM "Sea"."Events" e WHERE e."ProjectId" = p."Id" AND e."CommittedAt" IS NOT NULL AND e."DeletedAt" IS NULL) AS "CommittedEvents",

  (
    SELECT COUNT(*) FROM (
      SELECT a."Id" AS "ItemId"
      FROM "Sea"."Attachments" a
      JOIN "Sea"."Events" e ON e."Id" = a."EventId"
      WHERE e."ProjectId" = p."Id"
        AND a."DeletedAt" IS NULL
        AND e."DeletedAt" IS NULL
      UNION ALL
      SELECT r."Id" AS "ItemId"
      FROM "Communications"."Reminders" r
      JOIN "Sea"."Events" e ON e."Id" = r."SourceId"
      WHERE e."ProjectId" = p."Id"
        AND r."DeletedAt" IS NULL
        AND e."DeletedAt" IS NULL
    ) AS items
  ) AS "EventItems",

  (
    SELECT COUNT(*) FROM (
      SELECT a."Id" AS "ItemId"
      FROM "Sea"."Attachments" a
      JOIN "Sea"."Events" e ON e."Id" = a."EventId"
      WHERE e."ProjectId" = p."Id"
        AND a."DeletedAt" IS NULL
        AND e."DeletedAt" IS NULL
        AND NOT EXISTS (
          SELECT 1 FROM "Sea"."Receipts" rc
          WHERE rc."ObjectId" = a."Id"
            AND rc."CreatedBy" = current_setting('app.user_id', true)
        )
      UNION ALL
      SELECT r."Id" AS "ItemId"
      FROM "Communications"."Reminders" r
      JOIN "Sea"."Events" e ON e."Id" = r."SourceId"
      WHERE e."ProjectId" = p."Id"
        AND r."DeletedAt" IS NULL
        AND e."DeletedAt" IS NULL
        AND NOT EXISTS (
          SELECT 1 FROM "Sea"."Receipts" rc
          WHERE rc."ObjectId" = r."Id"
            AND rc."CreatedBy" = current_setting('app.user_id', true)
        )
    ) AS newitems
  ) AS "NewItems",

  (
    NOT EXISTS (
      SELECT 1 FROM "Sea"."Receipts" rc
      WHERE rc."ObjectId" = p."Id"
        AND rc."CreatedBy" = current_setting('app.user_id', true)
    )
    OR EXISTS (
      SELECT 1 FROM "Sea"."Procedures" pr2
      WHERE pr2."ProjectId" = p."Id"
        AND pr2."DeletedAt" IS NULL
        AND NOT EXISTS (
          SELECT 1 FROM "Sea"."Receipts" rc
          WHERE rc."ObjectId" = pr2."Id"
            AND rc."CreatedBy" = current_setting('app.user_id', true)
        )
    )
    OR EXISTS (
      SELECT 1 FROM "Sea"."Events" e2
      WHERE e2."ProjectId" = p."Id"
        AND e2."DeletedAt" IS NULL
        AND NOT EXISTS (
          SELECT 1 FROM "Sea"."Receipts" rc
          WHERE rc."ObjectId" = e2."Id"
            AND rc."CreatedBy" = current_setting('app.user_id', true)
        )
    )
    OR EXISTS (
      SELECT 1
      FROM "Sea"."Attachments" a
      JOIN "Sea"."Events" e ON e."Id" = a."EventId"
      WHERE e."ProjectId" = p."Id"
        AND a."DeletedAt" IS NULL
        AND e."DeletedAt" IS NULL
        AND NOT EXISTS (
          SELECT 1 FROM "Sea"."Receipts" rc
          WHERE rc."ObjectId" = a."Id"
            AND rc."CreatedBy" = current_setting('app.user_id', true)
        )
    )
    OR EXISTS (
      SELECT 1
      FROM "Communications"."Reminders" r
      JOIN "Sea"."Events" e ON e."Id" = r."SourceId"
      WHERE e."ProjectId" = p."Id"
        AND r."DeletedAt" IS NULL
        AND e."DeletedAt" IS NULL
        AND NOT EXISTS (
          SELECT 1 FROM "Sea"."Receipts" rc
          WHERE rc."ObjectId" = r."Id"
            AND rc."CreatedBy" = current_setting('app.user_id', true)
        )
    )
  ) AS "Unread",

  (
    SELECT MAX(v) FROM (
      SELECT GREATEST(p."CreatedAt", COALESCE(p."UpdatedAt", p."CreatedAt"), COALESCE(p."DeletedAt", p."CreatedAt")) AS v

      UNION ALL

      SELECT GREATEST(pr."CreatedAt", COALESCE(pr."UpdatedAt", pr."CreatedAt"), COALESCE(pr."DeletedAt", pr."CreatedAt"))
      FROM "Sea"."Procedures" pr
      WHERE pr."ProjectId" = p."Id"

      UNION ALL

      SELECT GREATEST(ev."CreatedAt", COALESCE(ev."UpdatedAt", ev."CreatedAt"), COALESCE(ev."DeletedAt", ev."CreatedAt"), COALESCE(ev."CommittedAt", ev."CreatedAt"))
      FROM "Sea"."Events" ev
      WHERE ev."ProjectId" = p."Id"

      UNION ALL

      SELECT GREATEST(a."CreatedAt", COALESCE(a."UpdatedAt", a."CreatedAt"), COALESCE(a."DeletedAt", a."CreatedAt"))
      FROM "Sea"."Attachments" a
      JOIN "Sea"."Events" ev2 ON ev2."Id" = a."EventId"
      WHERE ev2."ProjectId" = p."Id"

      UNION ALL

      SELECT GREATEST(r."CreatedAt", COALESCE(r."UpdatedAt", r."CreatedAt"), COALESCE(r."DeletedAt", r."CreatedAt"), COALESCE(r."SentAt", r."CreatedAt"))
      FROM "Communications"."Reminders" r
      JOIN "Sea"."Events" ev3 ON ev3."Id" = r."SourceId"
      WHERE ev3."ProjectId" = p."Id"
    ) AS t(v)
  ) AS "LastUpdate"

FROM "Sea"."Projects" p
WHERE p."DeletedAt" IS NULL;


/* ===================== vw_ProjectAttachments ===================== */
CREATE OR REPLACE VIEW "Sea"."vw_ProjectAttachments" AS
SELECT
  a."Id"               AS "AttachmentId",
  e."ProjectId"        AS "ProjectId",
  e."ProcedureId"      AS "ProcedureId",
  a."EventId"          AS "EventId",
  a."Content"          AS "Content",
  a."ContentType"      AS "ContentType",
  a."ContentThumbnail" AS "ContentThumbnail",
  a."CreatedAt"        AS "CreatedAt",
  a."CreatedBy"        AS "CreatedBy",
  a."TenantId"         AS "TenantId"
FROM "Sea"."Attachments" a
JOIN "Sea"."Events" e ON e."Id" = a."EventId"
JOIN "Sea"."Procedures" pr ON pr."Id" = e."ProcedureId"
JOIN "Sea"."Projects" p ON p."Id" = e."ProjectId"
WHERE a."DeletedAt" IS NULL
  AND e."DeletedAt" IS NULL
  AND pr."DeletedAt" IS NULL
  AND p."DeletedAt" IS NULL;


/* ===================== vw_ProcedureStats ===================== */
CREATE OR REPLACE VIEW "Sea"."vw_ProcedureStats" AS
SELECT
  pr."Id"        AS "ProcedureId",
  pr."ProjectId" AS "ProjectId",

  (SELECT COUNT(*) FROM "Sea"."Events" e WHERE e."ProcedureId" = pr."Id" AND e."DeletedAt" IS NULL) AS "Events",
  (SELECT COUNT(*) FROM "Sea"."Events" e WHERE e."ProcedureId" = pr."Id" AND e."CommittedAt" IS NOT NULL AND e."DeletedAt" IS NULL) AS "CommittedEvents",

  (
    SELECT COUNT(*) FROM (
      SELECT a."Id" AS "ItemId"
      FROM "Sea"."Attachments" a
      JOIN "Sea"."Events" e ON e."Id" = a."EventId"
      WHERE e."ProcedureId" = pr."Id"
        AND a."DeletedAt" IS NULL
        AND e."DeletedAt" IS NULL
      UNION ALL
      SELECT r."Id" AS "ItemId"
      FROM "Communications"."Reminders" r
      JOIN "Sea"."Events" e ON e."Id" = r."SourceId"
      WHERE e."ProcedureId" = pr."Id"
        AND r."DeletedAt" IS NULL
        AND e."DeletedAt" IS NULL
    ) AS items
  ) AS "EventItems",

  (
    SELECT COUNT(*) FROM (
      SELECT a."Id" AS "ItemId"
      FROM "Sea"."Attachments" a
      JOIN "Sea"."Events" e ON e."Id" = a."EventId"
      WHERE e."ProcedureId" = pr."Id"
        AND a."DeletedAt" IS NULL
        AND e."DeletedAt" IS NULL
        AND NOT EXISTS (
          SELECT 1 FROM "Sea"."Receipts" rc
          WHERE rc."ObjectId" = a."Id"
            AND rc."CreatedBy" = current_setting('app.user_id', true)
        )
      UNION ALL
      SELECT r."Id" AS "ItemId"
      FROM "Communications"."Reminders" r
      JOIN "Sea"."Events" e ON e."Id" = r."SourceId"
      WHERE e."ProcedureId" = pr."Id"
        AND r."DeletedAt" IS NULL
        AND e."DeletedAt" IS NULL
        AND NOT EXISTS (
          SELECT 1 FROM "Sea"."Receipts" rc
          WHERE rc."ObjectId" = r."Id"
            AND rc."CreatedBy" = current_setting('app.user_id', true)
        )
    ) AS newitems
  ) AS "NewItems",

  (
    NOT EXISTS (
      SELECT 1 FROM "Sea"."Receipts" rc
      WHERE rc."ObjectId" = pr."Id"
        AND rc."CreatedBy" = current_setting('app.user_id', true)
    )
    OR EXISTS (
      SELECT 1 FROM "Sea"."Events" e2
      WHERE e2."ProcedureId" = pr."Id"
        AND e2."DeletedAt" IS NULL
        AND NOT EXISTS (
          SELECT 1 FROM "Sea"."Receipts" rc
          WHERE rc."ObjectId" = e2."Id"
            AND rc."CreatedBy" = current_setting('app.user_id', true)
        )
    )
    OR EXISTS (
      SELECT 1
      FROM "Sea"."Attachments" a
      JOIN "Sea"."Events" e ON e."Id" = a."EventId"
      WHERE e."ProcedureId" = pr."Id"
        AND a."DeletedAt" IS NULL
        AND e."DeletedAt" IS NULL
        AND NOT EXISTS (
          SELECT 1 FROM "Sea"."Receipts" rc
          WHERE rc."ObjectId" = a."Id"
            AND rc."CreatedBy" = current_setting('app.user_id', true)
        )
    )
    OR EXISTS (
      SELECT 1
      FROM "Communications"."Reminders" r
      JOIN "Sea"."Events" e ON e."Id" = r."SourceId"
      WHERE e."ProcedureId" = pr."Id"
        AND r."DeletedAt" IS NULL
        AND e."DeletedAt" IS NULL
        AND NOT EXISTS (
          SELECT 1 FROM "Sea"."Receipts" rc
          WHERE rc."ObjectId" = r."Id"
            AND rc."CreatedBy" = current_setting('app.user_id', true)
        )
    )
  ) AS "Unread",

  (
    SELECT MAX(v) FROM (
      SELECT GREATEST(pr."CreatedAt", COALESCE(pr."UpdatedAt", pr."CreatedAt"), COALESCE(pr."DeletedAt", pr."CreatedAt")) AS v

      UNION ALL

      SELECT GREATEST(ev."CreatedAt", COALESCE(ev."UpdatedAt", ev."CreatedAt"), COALESCE(ev."DeletedAt", ev."CreatedAt"), COALESCE(ev."CommittedAt", ev."CreatedAt"))
      FROM "Sea"."Events" ev
      WHERE ev."ProcedureId" = pr."Id"

      UNION ALL

      SELECT GREATEST(a."CreatedAt", COALESCE(a."UpdatedAt", a."CreatedAt"), COALESCE(a."DeletedAt", a."CreatedAt"))
      FROM "Sea"."Attachments" a
      JOIN "Sea"."Events" ev2 ON ev2."Id" = a."EventId"
      WHERE ev2."ProcedureId" = pr."Id"

      UNION ALL

      SELECT GREATEST(r."CreatedAt", COALESCE(r."UpdatedAt", r."CreatedAt"), COALESCE(r."DeletedAt", r."CreatedAt"), COALESCE(r."SentAt", r."CreatedAt"))
      FROM "Communications"."Reminders" r
      JOIN "Sea"."Events" ev3 ON ev3."Id" = r."SourceId"
      WHERE ev3."ProcedureId" = pr."Id"
    ) AS t(v)
  ) AS "LastUpdate"

FROM "Sea"."Procedures" pr
WHERE pr."DeletedAt" IS NULL;


/* ===================== vw_EventStats ===================== */
CREATE OR REPLACE VIEW "Sea"."vw_EventStats" AS
SELECT
  e."Id" AS "EventId",

  (
    SELECT COUNT(*) FROM (
      SELECT a."Id" AS "ItemId"
      FROM "Sea"."Attachments" a
      WHERE a."EventId" = e."Id"
        AND a."DeletedAt" IS NULL
      UNION ALL
      SELECT r."Id" AS "ItemId"
      FROM "Communications"."Reminders" r
      WHERE r."SourceId" = e."Id"
        AND r."DeletedAt" IS NULL
    ) AS items
  ) AS "EventItems",

  (
    SELECT COUNT(*) FROM (
      SELECT a."Id" AS "ItemId"
      FROM "Sea"."Attachments" a
      WHERE a."EventId" = e."Id"
        AND a."DeletedAt" IS NULL
        AND NOT EXISTS (
          SELECT 1 FROM "Sea"."Receipts" rc
          WHERE rc."ObjectId" = a."Id"
            AND rc."CreatedBy" = current_setting('app.user_id', true)
        )
      UNION ALL
      SELECT r."Id" AS "ItemId"
      FROM "Communications"."Reminders" r
      WHERE r."SourceId" = e."Id"
        AND r."DeletedAt" IS NULL
        AND NOT EXISTS (
          SELECT 1 FROM "Sea"."Receipts" rc
          WHERE rc."ObjectId" = r."Id"
            AND rc."CreatedBy" = current_setting('app.user_id', true)
        )
    ) AS newitems
  ) AS "NewItems",

  (
    NOT EXISTS (
      SELECT 1 FROM "Sea"."Receipts" rc
      WHERE rc."ObjectId" = e."Id"
        AND rc."CreatedBy" = current_setting('app.user_id', true)
    )
    OR EXISTS (
      SELECT 1
      FROM "Sea"."Attachments" a
      WHERE a."EventId" = e."Id"
        AND a."DeletedAt" IS NULL
        AND NOT EXISTS (
          SELECT 1 FROM "Sea"."Receipts" rc
          WHERE rc."ObjectId" = a."Id"
            AND rc."CreatedBy" = current_setting('app.user_id', true)
        )
    )
    OR EXISTS (
      SELECT 1
      FROM "Communications"."Reminders" r
      WHERE r."SourceId" = e."Id"
        AND r."DeletedAt" IS NULL
        AND NOT EXISTS (
          SELECT 1 FROM "Sea"."Receipts" rc
          WHERE rc."ObjectId" = r."Id"
            AND rc."CreatedBy" = current_setting('app.user_id', true)
        )
    )
  ) AS "Unread",

  (
    SELECT MAX(v) FROM (
      SELECT GREATEST(e."CreatedAt", COALESCE(e."UpdatedAt", e."CreatedAt"), COALESCE(e."DeletedAt", e."CreatedAt"), COALESCE(e."CommittedAt", e."CreatedAt")) AS v

      UNION ALL

      SELECT GREATEST(a."CreatedAt", COALESCE(a."UpdatedAt", a."CreatedAt"), COALESCE(a."DeletedAt", a."CreatedAt"))
      FROM "Sea"."Attachments" a
      WHERE a."EventId" = e."Id"

      UNION ALL

      SELECT GREATEST(r."CreatedAt", COALESCE(r."UpdatedAt", r."CreatedAt"), COALESCE(r."DeletedAt", r."CreatedAt"), COALESCE(r."SentAt", r."CreatedAt"))
      FROM "Communications"."Reminders" r
      WHERE r."SourceId" = e."Id"
    ) AS t(v)
  ) AS "LastUpdate"

FROM "Sea"."Events" e
WHERE e."DeletedAt" IS NULL;
