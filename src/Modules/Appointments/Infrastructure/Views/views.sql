-- Notes:
-- Your migration runner doesn't recognize GO. To satisfy "CREATE VIEW must be the first statement in a batch",
-- each CREATE VIEW is now executed in its own dynamic batch via sp_executesql. You can run this whole file at once.

/* ===================== vw_SessionStats ===================== */
IF OBJECT_ID(N'dbo.vw_SessionStats', N'V') IS NOT NULL
    DROP VIEW dbo.vw_SessionStats;

EXEC sys.sp_executesql N'
CREATE VIEW dbo.vw_SessionStats AS
SELECT
  s.Id AS SessionId,

  (
    SELECT COUNT(*)
    FROM dbo.Appointments a
    WHERE a.SessionId = s.Id
      AND a.Status != 2 -- AppointmentStatus.Cancelled = 2
      AND a.DeletedAt IS NULL
  ) AS ReservationCount

FROM dbo.Sessions s
WHERE s.DeletedAt IS NULL;
';
