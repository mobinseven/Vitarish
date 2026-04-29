-- vw_SessionStats
-- Counts active (non-cancelled) appointments per session per tenant.
-- AppointmentStatus.Cancelled = 2

CREATE OR ALTER VIEW vw_SessionStats AS
SELECT
    SessionId,
    COUNT(*) AS ReservationCount
FROM Appointments
WHERE Status != 2 /* Cancelled */
  AND DeletedAt IS NULL
  AND SessionId IS NOT NULL
GROUP BY SessionId;
