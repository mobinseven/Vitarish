namespace Vitarish.Appointments.Infrastructure.Services;

[RegisterTransient]
internal class SessionBookingPolicyEvaluator
{
    public bool IsBookingOpen(Session session, DateOnly date)
        => SessionBookingRules.IsBookingOpen(session, date, DateTimeOffset.UtcNow);

    public bool CanCancel(Session session, Reservation reservation)
    {
        TimeSpan? deadline = session.BookingPolicy?.CancellationDeadline;
        if (deadline is null)
        {
            return true;
        }

        DateTimeOffset cutoff = ToDateTimeOffset(reservation.Date, session.StartTime) - deadline.Value;
        return DateTimeOffset.UtcNow < cutoff;
    }

    public bool CanReschedule(Session session)
        => session.BookingPolicy?.AllowRescheduling ?? false;

    public bool RequiresApproval(Session session)
        => session.BookingPolicy?.RequiresApproval ?? false;

    public bool NeedsPartyCapacityCheck(Session session, string? reservingPartyId)
        => !string.IsNullOrWhiteSpace(reservingPartyId)
            && session.BookingPolicy?.MaxReservationsPerParty is not null;

    public bool HasCapacityForParty(Session session, string? reservingPartyId, int currentReservationCount)
    {
        if (!NeedsPartyCapacityCheck(session, reservingPartyId))
        {
            return true;
        }

        return currentReservationCount < session.BookingPolicy!.MaxReservationsPerParty!.Value;
    }

    private static DateTimeOffset ToDateTimeOffset(DateOnly date, TimeOnly time)
        => new(date.ToDateTime(time), TimeSpan.Zero);
}
