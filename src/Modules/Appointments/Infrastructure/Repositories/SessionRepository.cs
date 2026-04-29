namespace Vitarish.Appointments.Infrastructure.Repositories;

[RegisterTransient]
internal class SessionRepository(AppointmentsDbContext dbContext)
    : EntityRepository<Session, SessionDescriptor, AppointmentsDbContext>(dbContext),
      IEntityRepository<Session, SessionDescriptor>
{
    protected override IQueryable<Session> Apply(IQueryable<Session> query, SessionDescriptor descriptor)
    {
        query = base.Apply(query, descriptor);

        if (descriptor.Status is { } status)
        {
            query = query.Where(s => s.Status == status);
        }

        if (descriptor.DateFrom is { } dateFrom)
        {
            query = query.Where(s => s.ScheduledAt >= dateFrom);
        }

        if (descriptor.DateTo is { } dateTo)
        {
            query = query.Where(s => s.ScheduledAt <= dateTo);
        }

        if (descriptor.SortBy is { } sort)
        {
            query = (sort, descriptor.Descending) switch
            {
                (SessionSortBy.ScheduledAt, true) => query.OrderByDescending(s => s.ScheduledAt),
                (SessionSortBy.ScheduledAt, false) => query.OrderBy(s => s.ScheduledAt),
                (SessionSortBy.Capacity, true) => query.OrderByDescending(s => s.Capacity),
                (SessionSortBy.Capacity, false) => query.OrderBy(s => s.Capacity),
                _ => query
            };
        }

        return query;
    }

    // ── Stats population ────────────────────────────────────────────────────────

    private async Task PopulateStatsAsync(IReadOnlyList<Session> sessions, CancellationToken cancellationToken)
    {
        if (sessions.Count == 0)
        {
            return;
        }

        List<Guid> sessionIds = [.. sessions.Select(s => s.Id)];

        Dictionary<Guid, SessionStats> stats = await dbContext.Set<SessionStats>()
            .AsNoTracking()
            .Where(stat => sessionIds.Contains(stat.SessionId))
            .ToDictionaryAsync(stat => stat.SessionId, cancellationToken);

        foreach (Session session in sessions)
        {
            session.Stats = stats.TryGetValue(session.Id, out SessionStats? stat)
                ? stat
                : new SessionStats { SessionId = session.Id };
        }
    }

    private Task PopulateStatsAsync(Session session, CancellationToken cancellationToken)
        => PopulateStatsAsync([session], cancellationToken);

    public new async Task<Session?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        Session? session = await base.GetAsync(id, cancellationToken);

        if (session is null)
        {
            return null;
        }

        await PopulateStatsAsync(session, cancellationToken);

        return session;
    }

    public new async Task<IReadOnlyList<Session>> GetAsync(SessionDescriptor descriptor, CancellationToken cancellationToken)
    {
        IReadOnlyList<Session> sessions = await base.GetAsync(descriptor, cancellationToken);

        if (sessions.Count > 0)
        {
            await PopulateStatsAsync(sessions, cancellationToken);
        }

        return sessions;
    }

    Task<Session?> IEntityRepository<Session, SessionDescriptor>.GetAsync(Guid id, CancellationToken cancellationToken)
        => GetAsync(id, cancellationToken);

    Task<IReadOnlyList<Session>> IEntityRepository<Session, SessionDescriptor>.GetAsync(SessionDescriptor descriptor, CancellationToken cancellationToken)
        => GetAsync(descriptor, cancellationToken);

    protected override bool IsDuplicateKeyException(DbUpdateException exception) => false;
}
