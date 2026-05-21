-- Notes:
-- Your migration runner doesn't recognize GO. To satisfy "CREATE VIEW must be the first statement in a batch",
-- each CREATE VIEW is now executed in its own dynamic batch via sp_executesql. You can run this whole file at once.
-- Other fixes kept: VALUES-based MAX, dbo-qualification, NVARCHAR session context cast, CROSS APPLY ordering.

/* ===================== vw_ProjectStats ===================== */
IF OBJECT_ID(N'dbo.vw_ProjectStats', N'V') IS NOT NULL
    DROP VIEW dbo.vw_ProjectStats;

EXEC sys.sp_executesql N'
CREATE VIEW dbo.vw_ProjectStats AS
SELECT
  p.Id AS ProjectId,

  (SELECT COUNT(*) FROM dbo.Procedures pr WHERE pr.ProjectId = p.Id AND pr.DeletedAt IS NULL) AS Procedures,
  (SELECT COUNT(*) FROM dbo.Events e WHERE e.ProjectId = p.Id AND e.DeletedAt IS NULL) AS Events,
  (SELECT COUNT(*) FROM dbo.Events e WHERE e.ProjectId = p.Id AND e.CommittedAt IS NOT NULL AND e.DeletedAt IS NULL) AS CommittedEvents,

  (
    SELECT COUNT(*) FROM (
      SELECT a.Id AS ItemId
      FROM dbo.Attachments a
      JOIN dbo.Events e ON e.Id = a.EventId
      WHERE e.ProjectId = p.Id
        AND a.DeletedAt IS NULL
        AND e.DeletedAt IS NULL
      UNION ALL
      SELECT r.Id AS ItemId
      FROM dbo.Reminders r
      JOIN dbo.Events e ON e.Id = r.SourceId
      WHERE e.ProjectId = p.Id
        AND r.DeletedAt IS NULL
        AND e.DeletedAt IS NULL
    ) AS items
  ) AS EventItems,

  (
    SELECT COUNT(*) FROM (
      SELECT a.Id AS ItemId
      FROM dbo.Attachments a
      JOIN dbo.Events e ON e.Id = a.EventId
      WHERE e.ProjectId = p.Id
        AND a.DeletedAt IS NULL
        AND e.DeletedAt IS NULL
        AND NOT EXISTS (
          SELECT 1 FROM dbo.Receipts rc
          WHERE rc.ObjectId = a.Id
            AND rc.CreatedBy = CONVERT(NVARCHAR(128), SESSION_CONTEXT(N''user_id''))
        )
      UNION ALL
      SELECT r.Id AS ItemId
      FROM dbo.Reminders r
      JOIN dbo.Events e ON e.Id = r.SourceId
      WHERE e.ProjectId = p.Id
        AND r.DeletedAt IS NULL
        AND e.DeletedAt IS NULL
        AND NOT EXISTS (
          SELECT 1 FROM dbo.Receipts rc
          WHERE rc.ObjectId = r.Id
            AND rc.CreatedBy = CONVERT(NVARCHAR(128), SESSION_CONTEXT(N''user_id''))
        )
    ) AS newitems
  ) AS NewItems,

  CASE
    WHEN
      NOT EXISTS (
        SELECT 1 FROM dbo.Receipts rc
        WHERE rc.ObjectId = p.Id
          AND rc.CreatedBy = CONVERT(NVARCHAR(128), SESSION_CONTEXT(N''user_id''))
      )
      OR EXISTS (
        SELECT 1 FROM dbo.Procedures pr2
        WHERE pr2.ProjectId = p.Id
          AND pr2.DeletedAt IS NULL
          AND NOT EXISTS (
            SELECT 1 FROM dbo.Receipts rc
            WHERE rc.ObjectId = pr2.Id
              AND rc.CreatedBy = CONVERT(NVARCHAR(128), SESSION_CONTEXT(N''user_id''))
          )
      )
      OR EXISTS (
        SELECT 1 FROM dbo.Events e2
        WHERE e2.ProjectId = p.Id
          AND e2.DeletedAt IS NULL
          AND NOT EXISTS (
            SELECT 1 FROM dbo.Receipts rc
            WHERE rc.ObjectId = e2.Id
              AND rc.CreatedBy = CONVERT(NVARCHAR(128), SESSION_CONTEXT(N''user_id''))
          )
      )
      OR EXISTS (
        SELECT 1
        FROM dbo.Attachments a
        JOIN dbo.Events e ON e.Id = a.EventId
        WHERE e.ProjectId = p.Id
          AND a.DeletedAt IS NULL
          AND e.DeletedAt IS NULL
          AND NOT EXISTS (
            SELECT 1 FROM dbo.Receipts rc
            WHERE rc.ObjectId = a.Id
              AND rc.CreatedBy = CONVERT(NVARCHAR(128), SESSION_CONTEXT(N''user_id''))
          )
      )
      OR EXISTS (
        SELECT 1
        FROM dbo.Reminders r
        JOIN dbo.Events e ON e.Id = r.SourceId
        WHERE e.ProjectId = p.Id
          AND r.DeletedAt IS NULL
          AND e.DeletedAt IS NULL
          AND NOT EXISTS (
            SELECT 1 FROM dbo.Receipts rc
            WHERE rc.ObjectId = r.Id
              AND rc.CreatedBy = CONVERT(NVARCHAR(128), SESSION_CONTEXT(N''user_id''))
          )
      )
    THEN CAST(1 AS bit) ELSE CAST(0 AS bit)
  END AS Unread,

  (
    SELECT MAX(value) FROM (
      SELECT MAX(v) AS value
      FROM (VALUES (
              p.CreatedAt
            ), (
              COALESCE(p.UpdatedAt, p.CreatedAt)
            ), (
              COALESCE(p.DeletedAt, p.CreatedAt)
            )) AS T(v)

      UNION ALL

      SELECT MAX(T2.v)
      FROM dbo.Procedures pr
      CROSS APPLY (VALUES (
                     pr.CreatedAt
                   ), (
                     COALESCE(pr.UpdatedAt, pr.CreatedAt)
                   ), (
                     COALESCE(pr.DeletedAt, pr.CreatedAt)
                   )) AS T2(v)
      WHERE pr.ProjectId = p.Id

      UNION ALL

      SELECT MAX(T3.v)
      FROM dbo.Events ev
      CROSS APPLY (VALUES (
                     ev.CreatedAt
                   ), (
                     COALESCE(ev.UpdatedAt, ev.CreatedAt)
                   ), (
                     COALESCE(ev.DeletedAt, ev.CreatedAt)
                   ), (
                     COALESCE(ev.CommittedAt, ev.CreatedAt)
                   )) AS T3(v)
      WHERE ev.ProjectId = p.Id

      UNION ALL

      SELECT MAX(T4.v)
      FROM dbo.Attachments a
      JOIN dbo.Events ev2 ON ev2.Id = a.EventId
      CROSS APPLY (VALUES (
                     a.CreatedAt
                   ), (
                     COALESCE(a.UpdatedAt, a.CreatedAt)
                   ), (
                     COALESCE(a.DeletedAt, a.CreatedAt)
                   )) AS T4(v)
      WHERE ev2.ProjectId = p.Id

      UNION ALL

      SELECT MAX(T5.v)
      FROM dbo.Reminders r
      JOIN dbo.Events ev3 ON ev3.Id = r.SourceId
      CROSS APPLY (VALUES (
                     r.CreatedAt
                   ), (
                     COALESCE(r.UpdatedAt, r.CreatedAt)
                   ), (
                     COALESCE(r.DeletedAt, r.CreatedAt)
                   ), (
                     COALESCE(r.SentAt, r.CreatedAt)
                   )) AS T5(v)
      WHERE ev3.ProjectId = p.Id
    ) AS S
  ) AS LastUpdate

FROM dbo.Projects p
WHERE p.DeletedAt IS NULL;
';


/* ===================== vw_ProjectAttachments ===================== */
IF OBJECT_ID(N'dbo.vw_ProjectAttachments', N'V') IS NOT NULL
    DROP VIEW dbo.vw_ProjectAttachments;

EXEC sys.sp_executesql N'
CREATE VIEW dbo.vw_ProjectAttachments AS
SELECT
  a.Id AS AttachmentId,
  e.ProjectId AS ProjectId,
  e.ProcedureId AS ProcedureId,
  a.EventId AS EventId,
  a.Content AS Content,
  a.ContentType AS ContentType,
  a.ContentThumbnail AS ContentThumbnail,
  a.CreatedAt AS CreatedAt,
  a.CreatedBy AS CreatedBy,
  a.TenantId AS TenantId
FROM dbo.Attachments a
JOIN dbo.Events e ON e.Id = a.EventId
JOIN dbo.Procedures pr ON pr.Id = e.ProcedureId
JOIN dbo.Projects p ON p.Id = e.ProjectId
WHERE a.DeletedAt IS NULL
  AND e.DeletedAt IS NULL
  AND pr.DeletedAt IS NULL
  AND p.DeletedAt IS NULL;
';

/* ===================== vw_ProcedureStats ===================== */
IF OBJECT_ID(N'dbo.vw_ProcedureStats', N'V') IS NOT NULL
    DROP VIEW dbo.vw_ProcedureStats;

EXEC sys.sp_executesql N'
CREATE VIEW dbo.vw_ProcedureStats AS
SELECT
  pr.Id AS ProcedureId,
  pr.ProjectId AS ProjectId,

  (SELECT COUNT(*) FROM dbo.Events e WHERE e.ProcedureId = pr.Id AND e.DeletedAt IS NULL) AS Events,
  (SELECT COUNT(*) FROM dbo.Events e WHERE e.ProcedureId = pr.Id AND e.CommittedAt IS NOT NULL AND e.DeletedAt IS NULL) AS CommittedEvents,

  (
    SELECT COUNT(*) FROM (
      SELECT a.Id AS ItemId
      FROM dbo.Attachments a
      JOIN dbo.Events e ON e.Id = a.EventId
      WHERE e.ProcedureId = pr.Id
        AND a.DeletedAt IS NULL
        AND e.DeletedAt IS NULL
      UNION ALL
      SELECT r.Id AS ItemId
      FROM dbo.Reminders r
      JOIN dbo.Events e ON e.Id = r.SourceId
      WHERE e.ProcedureId = pr.Id
        AND r.DeletedAt IS NULL
        AND e.DeletedAt IS NULL
    ) AS items
  ) AS EventItems,

  (
    SELECT COUNT(*) FROM (
      SELECT a.Id AS ItemId
      FROM dbo.Attachments a
      JOIN dbo.Events e ON e.Id = a.EventId
      WHERE e.ProcedureId = pr.Id
        AND a.DeletedAt IS NULL
        AND e.DeletedAt IS NULL
        AND NOT EXISTS (
          SELECT 1 FROM dbo.Receipts rc
          WHERE rc.ObjectId = a.Id
            AND rc.CreatedBy = CONVERT(NVARCHAR(128), SESSION_CONTEXT(N''user_id''))
        )
      UNION ALL
      SELECT r.Id AS ItemId
      FROM dbo.Reminders r
      JOIN dbo.Events e ON e.Id = r.SourceId
      WHERE e.ProcedureId = pr.Id
        AND r.DeletedAt IS NULL
        AND e.DeletedAt IS NULL
        AND NOT EXISTS (
          SELECT 1 FROM dbo.Receipts rc
          WHERE rc.ObjectId = r.Id
            AND rc.CreatedBy = CONVERT(NVARCHAR(128), SESSION_CONTEXT(N''user_id''))
        )
    ) AS newitems
  ) AS NewItems,

  CASE
    WHEN
      NOT EXISTS (
        SELECT 1 FROM dbo.Receipts rc
        WHERE rc.ObjectId = pr.Id
          AND rc.CreatedBy = CONVERT(NVARCHAR(128), SESSION_CONTEXT(N''user_id''))
      )
      OR EXISTS (
        SELECT 1 FROM dbo.Events e2
        WHERE e2.ProcedureId = pr.Id
          AND e2.DeletedAt IS NULL
          AND NOT EXISTS (
            SELECT 1 FROM dbo.Receipts rc
            WHERE rc.ObjectId = e2.Id
              AND rc.CreatedBy = CONVERT(NVARCHAR(128), SESSION_CONTEXT(N''user_id''))
          )
      )
      OR EXISTS (
        SELECT 1
        FROM dbo.Attachments a
        JOIN dbo.Events e ON e.Id = a.EventId
        WHERE e.ProcedureId = pr.Id
          AND a.DeletedAt IS NULL
          AND e.DeletedAt IS NULL
          AND NOT EXISTS (
            SELECT 1 FROM dbo.Receipts rc
            WHERE rc.ObjectId = a.Id
              AND rc.CreatedBy = CONVERT(NVARCHAR(128), SESSION_CONTEXT(N''user_id''))
          )
      )
      OR EXISTS (
        SELECT 1
        FROM dbo.Reminders r
        JOIN dbo.Events e ON e.Id = r.SourceId
        WHERE e.ProcedureId = pr.Id
          AND r.DeletedAt IS NULL
          AND e.DeletedAt IS NULL
          AND NOT EXISTS (
            SELECT 1 FROM dbo.Receipts rc
            WHERE rc.ObjectId = r.Id
              AND rc.CreatedBy = CONVERT(NVARCHAR(128), SESSION_CONTEXT(N''user_id''))
          )
      )
    THEN CAST(1 AS bit) ELSE CAST(0 AS bit)
  END AS Unread,

  (
    SELECT MAX(value) FROM (
      SELECT MAX(v) AS value
      FROM (VALUES (
              pr.CreatedAt
            ), (
              COALESCE(pr.UpdatedAt, pr.CreatedAt)
            ), (
              COALESCE(pr.DeletedAt, pr.CreatedAt)
            )) AS T(v)

      UNION ALL

      SELECT MAX(T2.v)
      FROM dbo.Events ev
      CROSS APPLY (VALUES (
                     ev.CreatedAt
                   ), (
                     COALESCE(ev.UpdatedAt, ev.CreatedAt)
                   ), (
                     COALESCE(ev.DeletedAt, ev.CreatedAt)
                   ), (
                     COALESCE(ev.CommittedAt, ev.CreatedAt)
                   )) AS T2(v)
      WHERE ev.ProcedureId = pr.Id

      UNION ALL

      SELECT MAX(T3.v)
      FROM dbo.Attachments a
      JOIN dbo.Events ev2 ON ev2.Id = a.EventId
      CROSS APPLY (VALUES (
                     a.CreatedAt
                   ), (
                     COALESCE(a.UpdatedAt, a.CreatedAt)
                   ), (
                     COALESCE(a.DeletedAt, a.CreatedAt)
                   )) AS T3(v)
      WHERE ev2.ProcedureId = pr.Id

      UNION ALL

      SELECT MAX(T4.v)
      FROM dbo.Reminders r
      JOIN dbo.Events ev3 ON ev3.Id = r.SourceId
      CROSS APPLY (VALUES (
                     r.CreatedAt
                   ), (
                     COALESCE(r.UpdatedAt, r.CreatedAt)
                   ), (
                     COALESCE(r.DeletedAt, r.CreatedAt)
                   ), (
                     COALESCE(r.SentAt, r.CreatedAt)
                   )) AS T4(v)
      WHERE ev3.ProcedureId = pr.Id
    ) AS S
  ) AS LastUpdate

FROM dbo.Procedures pr
WHERE pr.DeletedAt IS NULL;
';

/* ===================== vw_EventStats ===================== */
IF OBJECT_ID(N'dbo.vw_EventStats', N'V') IS NOT NULL
    DROP VIEW dbo.vw_EventStats;

EXEC sys.sp_executesql N'
CREATE VIEW dbo.vw_EventStats AS
SELECT
  e.Id AS EventId,

  (
    SELECT COUNT(*) FROM (
      SELECT a.Id AS ItemId
      FROM dbo.Attachments a
      WHERE a.EventId = e.Id
        AND a.DeletedAt IS NULL
      UNION ALL
      SELECT r.Id AS ItemId
      FROM dbo.Reminders r
      WHERE r.SourceId = e.Id
        AND r.DeletedAt IS NULL
    ) AS items
  ) AS EventItems,

  (
    SELECT COUNT(*) FROM (
      SELECT a.Id AS ItemId
      FROM dbo.Attachments a
      WHERE a.EventId = e.Id
        AND a.DeletedAt IS NULL
        AND NOT EXISTS (
          SELECT 1 FROM dbo.Receipts rc
          WHERE rc.ObjectId = a.Id
            AND rc.CreatedBy = CONVERT(NVARCHAR(128), SESSION_CONTEXT(N''user_id''))
        )
      UNION ALL
      SELECT r.Id AS ItemId
      FROM dbo.Reminders r
      WHERE r.SourceId = e.Id
        AND r.DeletedAt IS NULL
        AND NOT EXISTS (
          SELECT 1 FROM dbo.Receipts rc
          WHERE rc.ObjectId = r.Id
            AND rc.CreatedBy = CONVERT(NVARCHAR(128), SESSION_CONTEXT(N''user_id''))
        )
    ) AS newitems
  ) AS NewItems,

  CASE
    WHEN
      NOT EXISTS (
        SELECT 1 FROM dbo.Receipts rc
        WHERE rc.ObjectId = e.Id
          AND rc.CreatedBy = CONVERT(NVARCHAR(128), SESSION_CONTEXT(N''user_id''))
      )
      OR EXISTS (
        SELECT 1
        FROM dbo.Attachments a
        WHERE a.EventId = e.Id
          AND a.DeletedAt IS NULL
          AND NOT EXISTS (
            SELECT 1 FROM dbo.Receipts rc
            WHERE rc.ObjectId = a.Id
              AND rc.CreatedBy = CONVERT(NVARCHAR(128), SESSION_CONTEXT(N''user_id''))
          )
      )
      OR EXISTS (
        SELECT 1
        FROM dbo.Reminders r
        WHERE r.SourceId = e.Id
          AND r.DeletedAt IS NULL
          AND NOT EXISTS (
            SELECT 1 FROM dbo.Receipts rc
            WHERE rc.ObjectId = r.Id
              AND rc.CreatedBy = CONVERT(NVARCHAR(128), SESSION_CONTEXT(N''user_id''))
          )
      )
    THEN CAST(1 AS bit) ELSE CAST(0 AS bit)
  END AS Unread,

  (
    SELECT MAX(value) FROM (
      SELECT MAX(v) AS value
      FROM (VALUES (
              e.CreatedAt
            ), (
              COALESCE(e.UpdatedAt, e.CreatedAt)
            ), (
              COALESCE(e.DeletedAt, e.CreatedAt)
            ), (
              COALESCE(e.CommittedAt, e.CreatedAt)
            )) AS T(v)

      UNION ALL

      SELECT MAX(T2.v)
      FROM dbo.Attachments a
      CROSS APPLY (VALUES (
                     a.CreatedAt
                   ), (
                     COALESCE(a.UpdatedAt, a.CreatedAt)
                   ), (
                     COALESCE(a.DeletedAt, a.CreatedAt)
                   )) AS T2(v)
      WHERE a.EventId = e.Id

      UNION ALL

      SELECT MAX(T3.v)
      FROM dbo.Reminders r
      CROSS APPLY (VALUES (
                     r.CreatedAt
                   ), (
                     COALESCE(r.UpdatedAt, r.CreatedAt)
                   ), (
                     COALESCE(r.DeletedAt, r.CreatedAt)
                   ), (
                     COALESCE(r.SentAt, r.CreatedAt)
                   )) AS T3(v)
      WHERE r.SourceId = e.Id
    ) AS S
  ) AS LastUpdate

FROM dbo.Events e
WHERE e.DeletedAt IS NULL;
';
