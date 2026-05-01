namespace Vitarish.Appointments.Infrastructure.Repositories;

[RegisterTransient]
internal class AppointmentSessionRepository(AppointmentsDbContext dbContext)
    : EntityRepository<AppointmentSession, AppointmentSessionDescriptor, AppointmentsDbContext>(dbContext),
      IEntityRepository<AppointmentSession, AppointmentSessionDescriptor>
{
    protected override IQueryable<AppointmentSession> Apply(IQueryable<AppointmentSession> query, AppointmentSessionDescriptor descriptor)
    {
        query = base.Apply(query, descriptor);

        if (descriptor.Status is { } status)
        {
            query = query.Where(s => s.Status == status);
        }

        if (descriptor.DateFrom is { } dateFrom)
        {
            query = query.Where(s => s.DateTime >= dateFrom);
        }

        if (descriptor.DateTo is { } dateTo)
        {
            query = query.Where(s => s.DateTime <= dateTo);
        }

        if (descriptor.SortBy is { } sort)
        {
            query = (sort, descriptor.Descending) switch
            {
                (AppointmentSessionSortBy.DateTime, true) => query.OrderByDescending(s => s.DateTime),
                (AppointmentSessionSortBy.DateTime, false) => query.OrderBy(s => s.DateTime),
                (AppointmentSessionSortBy.Status, true) => query.OrderByDescending(s => s.Status),
                (AppointmentSessionSortBy.Status, false) => query.OrderBy(s => s.Status),
                (AppointmentSessionSortBy.Capacity, true) => query.OrderByDescending(s => s.Capacity),
                (AppointmentSessionSortBy.Capacity, false) => query.OrderBy(s => s.Capacity),
                _ => query
            };
        }

        return query;
    }

    protected override bool IsDuplicateKeyException(DbUpdateException exception) => false;

    // Explicit interface implementations intercept calls through IEntityRepository so that
    // every entity returned to callers (including MediatR generic handlers) already has Stats
    // populated from the Appointments table — no SQL view dependency required.

    async Task<AppointmentSession?> IEntityRepository<AppointmentSession, AppointmentSessionDescriptor>.GetAsync(
        Guid id, CancellationToken cancellationToken)
    {
        AppointmentSession? session = await base.GetAsync(id, cancellationToken);
        if (session is not null)
        {
            await PopulateStatsAsync([session], cancellationToken);
        }
        return session;
    }

    async Task<IReadOnlyList<AppointmentSession>> IEntityRepository<AppointmentSession, AppointmentSessionDescriptor>.GetAsync(
        AppointmentSessionDescriptor descriptor, CancellationToken cancellationToken)
    {
        IReadOnlyList<AppointmentSession> sessions = await base.GetAsync(descriptor, cancellationToken);
        await PopulateStatsAsync(sessions, cancellationToken);
        return sessions;
    }

    private async Task PopulateStatsAsync(IEnumerable<AppointmentSession> sessions, CancellationToken cancellationToken)
    {
        List<Guid> sessionIds = [.. sessions.Select(s => s.Id)];
        if (sessionIds.Count == 0)
        {
            return;
        }

        Dictionary<Guid, int> counts = await dbContext.Appointments
            .Where(a => a.SessionId != null
                     && sessionIds.Contains(a.SessionId.Value)
                     && a.Status != AppointmentStatus.Cancelled)
            .GroupBy(a => a.SessionId!.Value)
            .Select(g => new { SessionId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.SessionId, x => x.Count, cancellationToken);

        foreach (AppointmentSession session in sessions)
        {
            session.Stats = new AppointmentSessionStats
            {
                ReservationCount = counts.GetValueOrDefault(session.Id, 0)
            };
        }
    }
}
