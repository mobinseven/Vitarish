namespace Vitarish.Appointments.Infrastructure.Services;

[RegisterTransient]
internal class ShiftBookingPolicyEngine(
    AppointmentsDbContext dbContext) : IShiftBookingPolicyEngine
{
    public async Task<bool> IsBookingOpenAsync(Guid shiftId, DateOnly date, CancellationToken cancellationToken)
    {
        var data = await dbContext.Shifts
            .AsNoTracking()
            .Where(s => s.Id == shiftId && s.Status == ShiftStatus.Active)
            .Select(s => new
            {
                Shift = s,
                Policy = dbContext.ShiftBookingPolicies
                    .Where(p => p.ShiftId == shiftId && p.Date == date)
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (data is null)
        {
            return false;
        }

        IReadOnlyList<ShiftBookingPolicy> policies = data.Policy is not null
            ? [data.Policy]
            : [];

        return ShiftBookingPolicyEvaluator.IsBookingOpen(data.Shift, date, policies);
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
            .FirstOrDefaultAsync(s => s.Id == reservation.ShiftId, cancellationToken);

        if (shift is null)
        {
            return false;
        }

        IReadOnlyList<ShiftBookingPolicy> policies = await LoadPoliciesAsync(reservation.ShiftId, cancellationToken);
        return ShiftBookingPolicyEvaluator.CanCancel(shift, reservation, policies);
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
            .FirstOrDefaultAsync(s => s.Id == reservation.ShiftId, cancellationToken);

        if (shift is null)
        {
            return false;
        }

        IReadOnlyList<ShiftBookingPolicy> policies = await LoadPoliciesAsync(reservation.ShiftId, cancellationToken);
        return ShiftBookingPolicyEvaluator.CanReschedule(shift, reservation, policies);
    }

    public async Task<bool> HasCapacityForPartyAsync(Guid shiftId, DateOnly date, string? reservingPartyId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(reservingPartyId))
        {
            return true;
        }

        Task<Shift?> shiftTask = dbContext.Shifts
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == shiftId, cancellationToken);
        Task<IReadOnlyList<ShiftBookingPolicy>> policiesTask = LoadPoliciesAsync(shiftId, cancellationToken);

        await Task.WhenAll(shiftTask, policiesTask);

        Shift? shift = await shiftTask;
        if (shift is null)
        {
            return false;
        }

        IReadOnlyList<ShiftBookingPolicy> policies = await policiesTask;

        if (!ShiftBookingPolicyEvaluator.NeedsPartyCapacityCheck(shift, date, reservingPartyId, policies))
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

        return ShiftBookingPolicyEvaluator.HasCapacityForParty(shift, date, reservingPartyId, partyCount, policies);
    }

    public async Task<bool> RequiresApprovalAsync(Guid shiftId, DateOnly date, CancellationToken cancellationToken)
    {
        Task<Shift?> shiftTask = dbContext.Shifts
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == shiftId, cancellationToken);
        Task<IReadOnlyList<ShiftBookingPolicy>> policiesTask = LoadPoliciesAsync(shiftId, cancellationToken);

        await Task.WhenAll(shiftTask, policiesTask);

        Shift? shift = await shiftTask;
        if (shift is null)
        {
            return false;
        }

        IReadOnlyList<ShiftBookingPolicy> policies = await policiesTask;
        return ShiftBookingPolicyEvaluator.RequiresApproval(shift, date, policies);
    }

    private async Task<IReadOnlyList<ShiftBookingPolicy>> LoadPoliciesAsync(Guid shiftId, CancellationToken cancellationToken)
        => await dbContext.ShiftBookingPolicies
            .AsNoTracking()
            .Where(p => p.ShiftId == shiftId)
            .ToListAsync(cancellationToken);
}
