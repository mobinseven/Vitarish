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
        var result = await Set.AsNoTracking()
            .Where(s => s.Id == id)
            .Select(s => new
            {
                Entity = s,
                ReservationCount = dbContext.Reservations
                    .Count(r => dbContext.SessionOccurrences
                        .Where(o => o.SessionId == s.Id)
                        .Select(o => o.Id)
                        .Contains(r.SessionOccurrenceId)
                        && r.Status != ReservationStatus.Cancelled
                        && r.Status != ReservationStatus.Expired
                        && r.Status != ReservationStatus.Rejected
                        && !r.IsHold)
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (result is null)
        {
            return null;
        }

        result.Entity.Stats = new SessionStats { ReservationCount = result.ReservationCount };
        return result.Entity;
    }

    async Task<IReadOnlyList<Session>> IEntityRepository<Session, SessionDescriptor>.GetAsync(
        SessionDescriptor descriptor, CancellationToken cancellationToken)
    {
        var results = await ApplyCore(Set.AsNoTracking(), descriptor)
            .Select(s => new
            {
                Entity = s,
                ReservationCount = dbContext.Reservations
                    .Count(r => dbContext.SessionOccurrences
                        .Where(o => o.SessionId == s.Id)
                        .Select(o => o.Id)
                        .Contains(r.SessionOccurrenceId)
                        && r.Status != ReservationStatus.Cancelled
                        && r.Status != ReservationStatus.Expired
                        && r.Status != ReservationStatus.Rejected
                        && !r.IsHold)
            })
            .ToListAsync(cancellationToken);

        foreach (var result in results)
        {
            result.Entity.Stats = new SessionStats { ReservationCount = result.ReservationCount };
        }

        return results.ConvertAll(r => r.Entity);
    }
}
