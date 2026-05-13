namespace Vitarish.Appointments.Infrastructure.Services;

[RegisterTransient]
internal class ShiftBookingPolicyEngine(
    AppointmentsDbContext dbContext,
    ShiftBookingPolicyEvaluator policyEvaluator) : IShiftBookingPolicyEngine
{
    public async Task<bool> IsBookingOpenAsync(Guid shiftId, DateOnly date, CancellationToken cancellationToken)
    {
        Shift? shift = await dbContext.Shifts
            .AsNoTracking()
            .Include(s => s.BookingPolicy)
            .FirstOrDefaultAsync(s => s.Id == shiftId, cancellationToken);

        return shift is not null
            && policyEvaluator.IsBookingOpen(shift, date);
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

        Shift? shift = await dbContext.Shifts
            .AsNoTracking()
            .Include(s => s.BookingPolicy)
            .FirstOrDefaultAsync(s => s.Id == reservation.ShiftId, cancellationToken);

        if (shift is null)
        {
            return false;
        }

        return policyEvaluator.CanCancel(shift, reservation);
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

        Shift? shift = await dbContext.Shifts
            .AsNoTracking()
            .Include(s => s.BookingPolicy)
            .FirstOrDefaultAsync(s => s.Id == reservation.ShiftId, cancellationToken);

        return shift is not null && policyEvaluator.CanReschedule(shift);
    }

    public async Task<bool> HasCapacityForPartyAsync(Guid shiftId, DateOnly date, string? reservingPartyId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(reservingPartyId))
        {
            return true;
        }

        Shift? shift = await dbContext.Shifts
            .AsNoTracking()
            .Include(s => s.BookingPolicy)
            .FirstOrDefaultAsync(s => s.Id == shiftId, cancellationToken);

        if (shift is null)
        {
            return false;
        }

        if (!policyEvaluator.NeedsPartyCapacityCheck(shift, reservingPartyId))
        {
            return true;
        }

        int partyCount = await dbContext.Reservations
            .CountAsync(r => r.ShiftId == shiftId
                          && r.Date == date
                          && r.ReservingPartyId == reservingPartyId
                          && r.Status != ReservationStatus.Cancelled
                          && r.Status != ReservationStatus.Expired
                          && r.Status != ReservationStatus.Rejected,
                cancellationToken);

        return policyEvaluator.HasCapacityForParty(shift, reservingPartyId, partyCount);
    }

    public async Task<bool> RequiresApprovalAsync(Guid shiftId, CancellationToken cancellationToken)
    {
        Shift? shift = await dbContext.Shifts
            .AsNoTracking()
            .Include(s => s.BookingPolicy)
            .FirstOrDefaultAsync(s => s.Id == shiftId, cancellationToken);

        return shift is not null && policyEvaluator.RequiresApproval(shift);
    }
}
