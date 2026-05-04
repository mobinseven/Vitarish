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

        if (descriptor.Visibility is { } visibility)
        {
            query = query.Where(s => s.Visibility == visibility);
        }

        if (descriptor.OwnerId is { } ownerId)
        {
            query = query.Where(s => s.OwnerId == ownerId);
        }

        if (descriptor.CalendarMappingId is { } calendarMappingId)
        {
            query = query.Where(s => s.CalendarMappingId == calendarMappingId);
        }

        if (descriptor.SortBy is { } sort)
        {
            query = (sort, descriptor.Descending) switch
            {
                (SessionSortBy.Title, true) => query.OrderByDescending(s => s.Title),
                (SessionSortBy.Title, false) => query.OrderBy(s => s.Title),
                (SessionSortBy.Status, true) => query.OrderByDescending(s => s.Status),
                (SessionSortBy.Status, false) => query.OrderBy(s => s.Status),
                (SessionSortBy.Visibility, true) => query.OrderByDescending(s => s.Visibility),
                (SessionSortBy.Visibility, false) => query.OrderBy(s => s.Visibility),
                _ => query
            };
        }

        return query;
    }

    protected override bool IsDuplicateKeyException(DbUpdateException exception) => false;

    async Task<Session?> IEntityRepository<Session, SessionDescriptor>.GetAsync(
        Guid id, CancellationToken cancellationToken)
    {
        Session? session = await base.GetAsync(id, cancellationToken);
        if (session is not null)
        {
            await PopulateStatsAsync([session], cancellationToken);
        }
        return session;
    }

    async Task<IReadOnlyList<Session>> IEntityRepository<Session, SessionDescriptor>.GetAsync(
        SessionDescriptor descriptor, CancellationToken cancellationToken)
    {
        IReadOnlyList<Session> sessions = await base.GetAsync(descriptor, cancellationToken);
        await PopulateStatsAsync(sessions, cancellationToken);
        return sessions;
    }

    private async Task PopulateStatsAsync(IEnumerable<Session> sessions, CancellationToken cancellationToken)
    {
        List<Guid> sessionIds = [.. sessions.Select(s => s.Id)];
        if (sessionIds.Count == 0)
        {
            return;
        }

        Dictionary<Guid, int> counts = await dbContext.Reservations
            .Where(r => dbContext.SessionOccurrences
                .Where(o => sessionIds.Contains(o.SessionId))
                .Select(o => o.Id)
                .Contains(r.SessionOccurrenceId)
                && r.Status != ReservationStatus.Cancelled
                && r.Status != ReservationStatus.Expired
                && r.Status != ReservationStatus.Rejected
                && !r.IsHold)
            .Join(dbContext.SessionOccurrences,
                r => r.SessionOccurrenceId,
                o => o.Id,
                (r, o) => new { o.SessionId })
            .GroupBy(x => x.SessionId)
            .Select(g => new { SessionId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.SessionId, x => x.Count, cancellationToken);

        foreach (Session session in sessions)
        {
            session.Stats = new SessionStats
            {
                ReservationCount = counts.GetValueOrDefault(session.Id, 0)
            };
        }
    }
}
