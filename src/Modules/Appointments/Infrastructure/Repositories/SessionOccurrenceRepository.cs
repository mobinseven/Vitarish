namespace Vitarish.Appointments.Infrastructure.Repositories;

[RegisterTransient]
internal class SessionOccurrenceRepository(AppointmentsDbContext dbContext)
    : EntityRepository<SessionOccurrence, SessionOccurrenceDescriptor, AppointmentsDbContext>(dbContext),
      IEntityRepository<SessionOccurrence, SessionOccurrenceDescriptor>
{
    protected override IQueryable<SessionOccurrence> Apply(IQueryable<SessionOccurrence> query, SessionOccurrenceDescriptor descriptor)
    {
        query = base.Apply(query, descriptor);

        if (descriptor.SessionId is { } sessionId)
        {
            query = query.Where(o => o.SessionId == sessionId);
        }

        if (descriptor.DateFrom is { } dateFrom)
        {
            query = query.Where(o => o.OccurrenceStart >= dateFrom);
        }

        if (descriptor.DateTo is { } dateTo)
        {
            query = query.Where(o => o.OccurrenceStart <= dateTo);
        }

        if (descriptor.IsCancelled is { } isCancelled)
        {
            query = query.Where(o => o.IsCancelled == isCancelled);
        }

        return query;
    }

    protected override bool IsDuplicateKeyException(DbUpdateException exception) => false;

    async Task<SessionOccurrence?> IEntityRepository<SessionOccurrence, SessionOccurrenceDescriptor>.GetAsync(
        Guid id, CancellationToken cancellationToken)
    {
        var result = await Set.AsNoTracking()
            .Where(o => o.Id == id)
            .Select(o => new
            {
                Entity = o,
                Session = o.Session,
                ReservationCount = dbContext.Reservations
                    .Count(r => r.SessionOccurrenceId == o.Id
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

        result.Entity.Session = result.Session;
        result.Entity.Stats = new SessionOccurrenceStats { ReservationCount = result.ReservationCount };
        return result.Entity;
    }

    async Task<IReadOnlyList<SessionOccurrence>> IEntityRepository<SessionOccurrence, SessionOccurrenceDescriptor>.GetAsync(
        SessionOccurrenceDescriptor descriptor, CancellationToken cancellationToken)
    {
        IQueryable<SessionOccurrence> query = Apply(Set.AsNoTracking(), descriptor);

        if (query is not IOrderedQueryable<SessionOccurrence>)
        {
            query = query.OrderBy(o => o.OccurrenceStart);
        }

        var results = await query.ApplyPagination(descriptor)
            .Select(o => new
            {
                Entity = o,
                Session = o.Session,
                ReservationCount = dbContext.Reservations
                    .Count(r => r.SessionOccurrenceId == o.Id
                             && r.Status != ReservationStatus.Cancelled
                             && r.Status != ReservationStatus.Expired
                             && r.Status != ReservationStatus.Rejected
                             && !r.IsHold)
            })
            .ToListAsync(cancellationToken);

        foreach (var result in results)
        {
            result.Entity.Session = result.Session;
            result.Entity.Stats = new SessionOccurrenceStats { ReservationCount = result.ReservationCount };
        }

        return results.ConvertAll(r => r.Entity);
    }
}
