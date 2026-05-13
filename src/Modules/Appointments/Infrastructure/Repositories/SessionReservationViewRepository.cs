namespace Vitarish.Appointments.Infrastructure.Repositories;

internal interface ISessionReservationViewRepository
{
    Task<IReadOnlyList<SessionReservation>> GetAsync(
        IReadOnlyList<Guid> sessionIds, DateOnly dateFrom, DateOnly dateTo, CancellationToken cancellationToken);
}

[RegisterTransient]
internal sealed class SessionReservationViewRepository(AppointmentsDbContext dbContext) : ISessionReservationViewRepository
{
    public async Task<IReadOnlyList<SessionReservation>> GetAsync(
        IReadOnlyList<Guid> sessionIds, DateOnly dateFrom, DateOnly dateTo, CancellationToken cancellationToken)
        => await dbContext.Set<SessionReservation>()
            .AsNoTracking()
            .Where(sr => sessionIds.Contains(sr.SessionId) && sr.Date >= dateFrom && sr.Date <= dateTo)
            .ToListAsync(cancellationToken);
}
