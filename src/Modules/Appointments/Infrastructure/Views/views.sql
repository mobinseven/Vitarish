/* ===================== vw_SessionOccurrenceStats ===================== */
IF OBJECT_ID(N'dbo.vw_SessionOccurrenceStats', N'V') IS NOT NULL
    DROP VIEW dbo.vw_SessionOccurrenceStats;

EXEC sys.sp_executesql N'
CREATE VIEW dbo.vw_SessionOccurrenceStats AS
SELECT
    o.Id AS SessionOccurrenceId,
    COUNT(r.Id) AS ReservationCount
FROM dbo.SessionOccurrences o
LEFT JOIN dbo.Reservations r
    ON r.SessionOccurrenceId = o.Id
    AND r.Status NOT IN (2, 3, 4) /* Cancelled, Rejected, Expired */
    AND r.IsHold = 0
    AND r.DeletedAt IS NULL
WHERE o.DeletedAt IS NULL
GROUP BY o.Id;
';

/* ===================== vw_SessionStats ===================== */
IF OBJECT_ID(N'dbo.vw_SessionStats', N'V') IS NOT NULL
    DROP VIEW dbo.vw_SessionStats;

EXEC sys.sp_executesql N'
CREATE VIEW dbo.vw_SessionStats AS
SELECT
    s.Id AS SessionId,
    COUNT(r.Id) AS ReservationCount
FROM dbo.Sessions s
LEFT JOIN dbo.SessionOccurrences o
    ON o.SessionId = s.Id
    AND o.DeletedAt IS NULL
LEFT JOIN dbo.Reservations r
    ON r.SessionOccurrenceId = o.Id
    AND r.Status NOT IN (2, 3, 4) /* Cancelled, Rejected, Expired */
    AND r.IsHold = 0
    AND r.DeletedAt IS NULL
WHERE s.DeletedAt IS NULL
GROUP BY s.Id;
';
