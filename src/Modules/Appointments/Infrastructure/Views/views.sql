-- vw_OccurrenceStats
-- Counts active (non-cancelled, non-expired, non-rejected, non-hold) reservations per session occurrence per tenant.
-- ReservationStatus.Cancelled = 2, Rejected = 3, Expired = 4

CREATE OR ALTER VIEW vw_OccurrenceStats AS
SELECT
    SessionOccurrenceId,
    COUNT(*) AS ReservationCount
FROM Reservations
WHERE Status NOT IN (2, 3, 4) /* Cancelled, Rejected, Expired */
  AND IsHold = 0
  AND DeletedAt IS NULL
GROUP BY SessionOccurrenceId;
