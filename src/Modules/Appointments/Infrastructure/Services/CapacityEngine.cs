namespace Vitarish.Appointments.Infrastructure.Services;

[RegisterTransient]
internal class CapacityEngine(AppointmentsDbContext dbContext) : ICapacityEngine
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

        return occurrence.Capacity;
    }

    public async Task<int> GetAvailableSeatsAsync(Guid sessionOccurrenceId, CancellationToken cancellationToken)
    {
        int capacity = await GetEffectiveCapacityAsync(sessionOccurrenceId, cancellationToken);

        int occupied = await dbContext.Reservations
            .Where(r => r.SessionOccurrenceId == sessionOccurrenceId
                     && r.Status != ReservationStatus.Cancelled
                     && r.Status != ReservationStatus.Expired
                     && r.Status != ReservationStatus.Rejected
                     && r.Status != ReservationStatus.Waitlisted
                     && !r.IsHold)
            .CountAsync(cancellationToken);

        return Math.Max(0, capacity - occupied);
    }

    public async Task<bool> IsAvailableAsync(Guid sessionOccurrenceId, CancellationToken cancellationToken)
        => await GetAvailableSeatsAsync(sessionOccurrenceId, cancellationToken) > 0;
}
