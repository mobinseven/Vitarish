namespace Vitarish.Appointments.Infrastructure.Services;

[RegisterTransient]
internal class SessionBookingPolicyEngine(AppointmentsDbContext dbContext) : ISessionBookingPolicyEngine
{
    public async Task<bool> IsBookingOpenAsync(Guid sessionId, DateOnly date, CancellationToken cancellationToken)
    {
        Session? session = await dbContext.Sessions
            .AsNoTracking()
            .Include(s => s.BookingPolicy)
            .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);

        return session is not null
            && SessionBookingRules.IsBookingOpen(session, date, DateTimeOffset.UtcNow);
    }

    public async Task<bool> CanCancelAsync(Guid reservationId, CancellationToken cancellationToken)
    {
        Reservation? reservation = await dbContext.Reservations
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == reservationId, cancellationToken);

        if (reservation is null)
        {
            return false;
        }

        Session? session = await dbContext.Sessions
            .AsNoTracking()
            .Include(s => s.BookingPolicy)
            .FirstOrDefaultAsync(s => s.Id == reservation.SessionId, cancellationToken);

        if (session is null)
        {
            return false;
        }

        TimeSpan? deadline = session.BookingPolicy?.CancellationDeadline;
        if (deadline is null)
        {
            return true;
        }

        DateTimeOffset cutoff = ToDateTimeOffset(reservation.Date, session.StartTime) - deadline.Value;
        return DateTimeOffset.UtcNow < cutoff;
    }

    public async Task<bool> CanRescheduleAsync(Guid reservationId, CancellationToken cancellationToken)
    {
        Reservation? reservation = await dbContext.Reservations
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == reservationId, cancellationToken);

        if (reservation is null)
        {
            return false;
        }

        Session? session = await dbContext.Sessions
            .AsNoTracking()
            .Include(s => s.BookingPolicy)
            .FirstOrDefaultAsync(s => s.Id == reservation.SessionId, cancellationToken);

        return session?.BookingPolicy?.AllowRescheduling ?? false;
    }

    public async Task<bool> HasCapacityForPartyAsync(Guid sessionId, DateOnly date, string? reservingPartyId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(reservingPartyId))
        {
            return true;
        }

        Session? session = await dbContext.Sessions
            .AsNoTracking()
            .Include(s => s.BookingPolicy)
            .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);

        if (session is null)
        {
            return false;
        }

        int? maxPerParty = session.BookingPolicy?.MaxReservationsPerParty;
        if (maxPerParty is null)
        {
            return true;
        }

        int partyCount = await dbContext.Reservations
            .CountAsync(r => r.SessionId == sessionId
                          && r.Date == date
                          && r.ReservingPartyId == reservingPartyId
                          && r.Status != ReservationStatus.Cancelled
                          && r.Status != ReservationStatus.Expired
                          && r.Status != ReservationStatus.Rejected,
                cancellationToken);

        return partyCount < maxPerParty.Value;
    }

    public async Task<bool> RequiresApprovalAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        Session? session = await dbContext.Sessions
            .AsNoTracking()
            .Include(s => s.BookingPolicy)
            .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);

        return session?.BookingPolicy?.RequiresApproval ?? false;
    }

    private static DateTimeOffset ToDateTimeOffset(DateOnly date, TimeOnly time)
        => new(date.ToDateTime(time), TimeSpan.Zero);
}
