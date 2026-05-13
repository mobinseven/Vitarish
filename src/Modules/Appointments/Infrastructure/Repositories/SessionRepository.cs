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

        if (descriptor.DayOfWeek is { } dayOfWeek)
        {
            query = query.Where(s => s.DayOfWeek == dayOfWeek);
        }

        if (descriptor.StartTime is { } startTime)
        {
            query = query.Where(s => s.StartTime == startTime);
        }

        if (descriptor.EndTime is { } endTime)
        {
            query = query.Where(s => s.EndTime == endTime);
        }

        if (descriptor.SortBy is { } sort)
        {
            query = (sort, descriptor.Descending) switch
            {
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
        Session? session = await dbContext.Sessions
            .AsNoTracking()
            .Include(s => s.BookingPolicy)
            .Include(s => s.ReminderPolicies)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (session is not null)
        {
            await PopulateStatsAsync([session], cancellationToken);
        }
        return session;
    }

    //async Task<IReadOnlyList<Session>> IEntityRepository<Session, SessionDescriptor>.GetAsync(
    //    SessionDescriptor descriptor, CancellationToken cancellationToken)
    //{
    //    IQueryable<Session> query = Apply(
    //        dbContext.Sessions
    //            .AsNoTracking()
    //            .Include(s => s.BookingPolicy)
    //            .Include(s => s.ReminderPolicies),
    //        descriptor);

    //    if (query is not IOrderedQueryable<Session>)
    //    {
    //        query = query.OrderByDescending(s => s.UpdatedAt ?? s.CreatedAt);
    //    }

    //    IReadOnlyList<Session> sessions = await query.ApplyPagination(descriptor).ToListAsync(cancellationToken);
    //    await PopulateStatsAsync(sessions, cancellationToken);
    //    return sessions;
    //}

    private async Task PopulateStatsAsync(IEnumerable<Session> sessions, CancellationToken cancellationToken)
    {
        List<Guid> sessionIds = [.. sessions.Select(s => s.Id)];
        if (sessionIds.Count == 0)
        {
            return;
        }

        Dictionary<Guid, SessionStats> stats = await dbContext.Set<SessionStats>()
            .AsNoTracking()
            .Where(stat => sessionIds.Contains(stat.SessionId))
            .ToDictionaryAsync(stat => stat.SessionId, cancellationToken);

        foreach (Session session in sessions)
        {
            session.Stats = stats.TryGetValue(session.Id, out SessionStats? stat)
                ? stat
                : new SessionStats
                {
                    SessionId = session.Id,
                    ReservationCount = 0
                };
        }
    }
}
