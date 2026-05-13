namespace Vitarish.Appointments.Infrastructure.Services;

[RegisterTransient]
internal class SessionBookingPolicyEngine(
    AppointmentsDbContext dbContext,
    SessionBookingPolicyEvaluator policyEvaluator) : ISessionBookingPolicyEngine
{
    public async Task<bool> IsBookingOpenAsync(Guid sessionId, DateOnly date, CancellationToken cancellationToken)
    {
        Session? session = await dbContext.Sessions
            .AsNoTracking()
            .Include(s => s.BookingPolicy)
            .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);

        return session is not null
            && policyEvaluator.IsBookingOpen(session, date);
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

        return policyEvaluator.CanCancel(session, reservation);
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

        return session is not null && policyEvaluator.CanReschedule(session);
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

        if (!policyEvaluator.NeedsPartyCapacityCheck(session, reservingPartyId))
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

        return policyEvaluator.HasCapacityForParty(session, reservingPartyId, partyCount);
    }

    public async Task<bool> RequiresApprovalAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        Session? session = await dbContext.Sessions
            .AsNoTracking()
            .Include(s => s.BookingPolicy)
            .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);

        return session is not null && policyEvaluator.RequiresApproval(session);
    }
}
