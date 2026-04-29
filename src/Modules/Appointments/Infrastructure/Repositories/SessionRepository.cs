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

    protected override bool IsDuplicateKeyException(DbUpdateException exception) => false;
}
