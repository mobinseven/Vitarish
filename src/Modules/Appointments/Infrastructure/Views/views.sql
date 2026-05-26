/* ===================== vw_OpenBookingShiftDates ===================== */
IF OBJECT_ID(N'Appointments.vw_OpenBookingShiftDates', N'V') IS NOT NULL
    DROP VIEW Appointments.vw_OpenBookingShiftDates;

EXEC sys.sp_executesql N'
CREATE VIEW Appointments.vw_OpenBookingShiftDates AS
WITH
Numbers AS (
    SELECT TOP 365 ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) - 1 AS N
    FROM sys.all_objects
),
FutureDates AS (
    SELECT CAST(DATEADD(day, N, CAST(GETUTCDATE() AS date)) AS date) AS Date
    FROM Numbers
),
ShiftDates AS (
    SELECT
        s.Id                 AS ShiftId,
        fd.Date,
        s.StartTime,
        s.BookingOpensBefore AS ShiftOpensBefore,
        s.BookingCloseBefore AS ShiftCloseBefore
    FROM Appointments.Shifts s
    CROSS JOIN FutureDates fd
    WHERE s.DeletedAt IS NULL
      AND s.Status = 1  -- ShiftStatus.Active
      -- Map .NET DayOfWeek (0=Sun..6=Sat) to SQL regardless of SET DATEFIRST
      AND s.DayOfWeek = (DATEPART(dw, fd.Date) + @@DATEFIRST - 1) % 7
)
SELECT sd.ShiftId, sd.Date
FROM ShiftDates sd
LEFT JOIN Appointments.ShiftBookingPolicies po
    ON  po.ShiftId   = sd.ShiftId
    AND po.Date      = sd.Date
    AND po.DeletedAt IS NULL
WHERE
    -- Not explicitly closed by a per-date policy
    ISNULL(po.Closed, 0) = 0
    -- Slot has not yet started.
    -- StartTime is stored in IANA local time; SQL Server does not support IANA zone
    -- conversion natively, so StartTime is compared against UTC as an approximation.
    AND GETUTCDATE() < CONVERT(datetime2(7),
            CONVERT(char(10), sd.Date, 120) + ''T'' + CONVERT(char(8), sd.StartTime, 108))
    -- BookingOpensBefore: booking opens N days before the slot date
    AND (
        COALESCE(po.BookingOpensBefore, sd.ShiftOpensBefore) IS NULL
        OR GETUTCDATE() >= DATEADD(day,
               -COALESCE(po.BookingOpensBefore, sd.ShiftOpensBefore),
               CAST(sd.Date AS datetime2(7)))
    )
    -- BookingCloseBefore: booking closes N days before the slot date
    AND (
        COALESCE(po.BookingCloseBefore, sd.ShiftCloseBefore) IS NULL
        OR GETUTCDATE() < DATEADD(day,
              -COALESCE(po.BookingCloseBefore, sd.ShiftCloseBefore),
              CAST(sd.Date AS datetime2(7)))
    )
';

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
