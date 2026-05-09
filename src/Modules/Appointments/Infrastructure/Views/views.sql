/* ===================== vw_SessionOccurrenceStats ===================== */
IF OBJECT_ID(N'dbo.vw_SessionOccurrenceStats', N'V') IS NOT NULL
    DROP VIEW dbo.vw_SessionOccurrenceStats;

EXEC sys.sp_executesql N'
CREATE VIEW dbo.vw_SessionOccurrenceStats AS
WITH ExcludedReservationStatuses(Status) AS
(
    /* ReservationStatus: Cancelled = 2, Rejected = 3, Expired = 4 */
    SELECT Status
    FROM (VALUES (2), (3), (4)) AS ReservationStatusValues(Status)
),
ActiveReservations AS
(
    SELECT r.Id, r.SessionOccurrenceId
    FROM dbo.Reservations r
    WHERE r.Status NOT IN (SELECT Status FROM ExcludedReservationStatuses)
        AND r.IsHold = 0
        AND r.DeletedAt IS NULL
)
SELECT
    o.Id AS SessionOccurrenceId,
    COUNT(r.Id) AS ReservationCount
FROM dbo.SessionOccurrences o
LEFT JOIN ActiveReservations r
    ON r.SessionOccurrenceId = o.Id
WHERE o.DeletedAt IS NULL
GROUP BY o.Id;
';

/* ===================== vw_SessionStats ===================== */
IF OBJECT_ID(N'dbo.vw_SessionStats', N'V') IS NOT NULL
    DROP VIEW dbo.vw_SessionStats;

EXEC sys.sp_executesql N'
CREATE VIEW dbo.vw_SessionStats AS
WITH ExcludedReservationStatuses(Status) AS
(
    /* ReservationStatus: Cancelled = 2, Rejected = 3, Expired = 4 */
    SELECT Status
    FROM (VALUES (2), (3), (4)) AS ReservationStatusValues(Status)
),
ActiveReservations AS
(
    SELECT r.Id, r.SessionOccurrenceId
    FROM dbo.Reservations r
    WHERE r.Status NOT IN (SELECT Status FROM ExcludedReservationStatuses)
        AND r.IsHold = 0
        AND r.DeletedAt IS NULL
)
SELECT
    s.Id AS SessionId,
    COUNT(r.Id) AS ReservationCount
FROM dbo.Sessions s
LEFT JOIN dbo.SessionOccurrences o
    ON o.SessionId = s.Id
    AND o.DeletedAt IS NULL
LEFT JOIN ActiveReservations r
    ON r.SessionOccurrenceId = o.Id
WHERE s.DeletedAt IS NULL
GROUP BY s.Id;
';
