/* ===================== vw_ShiftReservation ===================== */
IF OBJECT_ID(N'Appointments.vw_ShiftReservation', N'V') IS NOT NULL
    DROP VIEW Appointments.vw_ShiftReservation;

EXEC sys.sp_executesql N'
CREATE VIEW Appointments.vw_ShiftReservation AS
WITH ExcludedReservationStatuses(Status) AS
(
    /* ReservationStatus: Cancelled = 2, Rejected = 3, Expired = 4 */
    SELECT Status
    FROM (VALUES (2), (3), (4)) AS ReservationStatusValues(Status)
)
SELECT
    s.Id AS ShiftId,
    r.Date,
    s.StartTime,
    s.EndTime,
    s.TotalSeats,
    COUNT(r.Id) AS ReservationCount,
    CASE WHEN s.TotalSeats - COUNT(r.Id) < 0 THEN 0 ELSE s.TotalSeats - COUNT(r.Id) END AS AvailableSeats
FROM Appointments.Shifts s
LEFT JOIN Appointments.Reservations r
    ON r.ShiftId = s.Id
    AND r.Status NOT IN (SELECT Status FROM ExcludedReservationStatuses)
    AND r.IsHold = 0
    AND r.DeletedAt IS NULL
WHERE s.DeletedAt IS NULL
GROUP BY s.Id, r.Date, s.StartTime, s.EndTime, s.TotalSeats;
';

/* ===================== vw_ShiftStats ===================== */
IF OBJECT_ID(N'Appointments.vw_ShiftStats', N'V') IS NOT NULL
    DROP VIEW Appointments.vw_ShiftStats;

EXEC sys.sp_executesql N'
CREATE VIEW Appointments.vw_ShiftStats AS
WITH ExcludedReservationStatuses(Status) AS
(
    /* ReservationStatus: Cancelled = 2, Rejected = 3, Expired = 4 */
    SELECT Status
    FROM (VALUES (2), (3), (4)) AS ReservationStatusValues(Status)
)
SELECT
    s.Id AS ShiftId,
    COUNT(r.Id) AS ReservationCount
FROM Appointments.Shifts s
LEFT JOIN Appointments.Reservations r
    ON r.ShiftId = s.Id
    AND r.Status NOT IN (SELECT Status FROM ExcludedReservationStatuses)
    AND r.IsHold = 0
    AND r.DeletedAt IS NULL
WHERE s.DeletedAt IS NULL
GROUP BY s.Id;
';
