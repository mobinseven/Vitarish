namespace Vitarish.Appointments.Infrastructure.Services;

[RegisterTransient]
internal class CapacityEngine(
    CapacityPolicyRepository capacityPolicyRepository,
    AppointmentsDbContext dbContext) : ICapacityEngine
{
    public async Task<int> GetEffectiveCapacityAsync(Guid sessionOccurrenceId, CancellationToken cancellationToken)
    {
        SessionOccurrence? occurrence = await dbContext.SessionOccurrences
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == sessionOccurrenceId, cancellationToken);

        if (occurrence is null)
        {
            return 0;
        }

        if (occurrence.CapacityOverride.HasValue)
        {
            return occurrence.CapacityOverride.Value;
        }

        CapacityPolicy? policy = await capacityPolicyRepository.GetBySessionIdAsync(occurrence.SessionId, cancellationToken);
        if (policy is not null)
        {
            return policy.TotalSeats;
        }

        Session? session = await dbContext.Sessions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == occurrence.SessionId, cancellationToken);

        return session?.TotalSeats ?? 0;
    }

    public async Task<int> GetAvailableSeatsAsync(Guid sessionOccurrenceId, CancellationToken cancellationToken)
    {
        int capacity = await GetEffectiveCapacityAsync(sessionOccurrenceId, cancellationToken);

        SessionOccurrence? occurrence = await dbContext.SessionOccurrences
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == sessionOccurrenceId, cancellationToken);

        if (occurrence is null)
        {
            return 0;
        }

        CapacityPolicy? policy = await capacityPolicyRepository.GetBySessionIdAsync(occurrence.SessionId, cancellationToken);
        bool pendingConsumes = policy?.PendingConsumesCapacity ?? true;

        int occupied = await dbContext.Reservations
            .Where(r => r.SessionOccurrenceId == sessionOccurrenceId
                     && r.Status != ReservationStatus.Cancelled
                     && r.Status != ReservationStatus.Expired
                     && r.Status != ReservationStatus.Rejected
                     && r.Status != ReservationStatus.Waitlisted
                     && !r.IsHold
                     && (pendingConsumes || r.Status != ReservationStatus.Pending))
            .CountAsync(cancellationToken);

        return Math.Max(0, capacity - occupied);
    }

    public async Task<bool> IsAvailableAsync(Guid sessionOccurrenceId, CancellationToken cancellationToken)
        => await GetAvailableSeatsAsync(sessionOccurrenceId, cancellationToken) > 0;
}
