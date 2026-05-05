namespace Vitarish.Appointments.Infrastructure.Services;

[RegisterTransient]
internal class BookingPolicyEngine(
    BookingPolicyRepository bookingPolicyRepository,
    AppointmentsDbContext dbContext) : IBookingPolicyEngine
{
    public async Task<bool> IsBookingOpenAsync(Guid sessionOccurrenceId, CancellationToken cancellationToken)
    {
        SessionOccurrence? occurrence = await dbContext.SessionOccurrences
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == sessionOccurrenceId, cancellationToken);

        if (occurrence is null || occurrence.IsCancelled)
        {
            return false;
        }

        BookingPolicy? policy = await bookingPolicyRepository.GetBySessionIdAsync(occurrence.SessionId, cancellationToken);
        DateTimeOffset now = DateTimeOffset.UtcNow;

        if (policy is null)
        {
            return now < occurrence.OccurrenceStart;
        }

        if (policy.BookingOpensBefore.HasValue)
        {
            DateTimeOffset opensAt = occurrence.OccurrenceStart - policy.BookingOpensBefore.Value;
            if (now < opensAt)
            {
                return false;
            }
        }

        if (policy.BookingCloseBefore.HasValue)
        {
            DateTimeOffset closesAt = occurrence.OccurrenceStart - policy.BookingCloseBefore.Value;
            if (now >= closesAt)
            {
                return false;
            }
        }

        return true;
    }

    public async Task<bool> CanCancelAsync(Guid reservationId, CancellationToken cancellationToken)
    {
        Reservation? reservation = await dbContext.Reservations
            .AsNoTracking()
            .Include(r => r.Occurrence)
                .ThenInclude(o => o!.Session)
            .FirstOrDefaultAsync(r => r.Id == reservationId, cancellationToken);

        if (reservation?.Occurrence is null)
        {
            return false;
        }

        BookingPolicy? policy = await bookingPolicyRepository.GetBySessionIdAsync(reservation.Occurrence.SessionId, cancellationToken);

        if (policy?.CancellationDeadline is null)
        {
            return true;
        }

        DateTimeOffset deadline = reservation.Occurrence.OccurrenceStart - policy.CancellationDeadline.Value;
        return DateTimeOffset.UtcNow < deadline;
    }

    public async Task<bool> CanRescheduleAsync(Guid reservationId, CancellationToken cancellationToken)
    {
        Reservation? reservation = await dbContext.Reservations
            .AsNoTracking()
            .Include(r => r.Occurrence)
                .ThenInclude(o => o!.Session)
            .FirstOrDefaultAsync(r => r.Id == reservationId, cancellationToken);

        if (reservation?.Occurrence is null)
        {
            return false;
        }

        BookingPolicy? policy = await bookingPolicyRepository.GetBySessionIdAsync(reservation.Occurrence.SessionId, cancellationToken);
        return policy?.AllowRescheduling ?? reservation.Occurrence.Session?.AllowRescheduling ?? false;
    }

    public async Task<bool> HasCapacityForPartyAsync(Guid sessionOccurrenceId, string? reservingPartyId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(reservingPartyId))
        {
            return true;
        }

        SessionOccurrence? occurrence = await dbContext.SessionOccurrences
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == sessionOccurrenceId, cancellationToken);

        if (occurrence is null)
        {
            return false;
        }

        BookingPolicy? policy = await bookingPolicyRepository.GetBySessionIdAsync(occurrence.SessionId, cancellationToken);

        if (policy?.MaxReservationsPerParty is null)
        {
            return true;
        }

        int partyCount = await dbContext.Reservations
            .Where(r => r.SessionOccurrenceId == sessionOccurrenceId
                     && r.ReservingPartyId == reservingPartyId
                     && r.Status != ReservationStatus.Cancelled
                     && r.Status != ReservationStatus.Expired
                     && r.Status != ReservationStatus.Rejected)
            .CountAsync(cancellationToken);

        return partyCount < policy.MaxReservationsPerParty.Value;
    }
}
