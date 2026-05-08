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
        SessionOccurrence? occurrence = await Set.AsNoTracking()
            .Include(o => o.Session)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

        if (occurrence is not null)
        {
            await PopulateStatsAsync([occurrence], cancellationToken);
        }

        return occurrence;
    }

    async Task<IReadOnlyList<SessionOccurrence>> IEntityRepository<SessionOccurrence, SessionOccurrenceDescriptor>.GetAsync(
        SessionOccurrenceDescriptor descriptor, CancellationToken cancellationToken)
    {
        IQueryable<SessionOccurrence> query = Apply(Set.AsNoTracking().Include(o => o.Session), descriptor);

        if (query is not IOrderedQueryable<SessionOccurrence>)
        {
            query = query.OrderBy(o => o.OccurrenceStart);
        }

        IReadOnlyList<SessionOccurrence> occurrences = await query.ApplyPagination(descriptor).ToListAsync(cancellationToken);
        await PopulateStatsAsync(occurrences, cancellationToken);
        return occurrences;
    }

    private async Task PopulateStatsAsync(IEnumerable<SessionOccurrence> occurrences, CancellationToken cancellationToken)
    {
        List<Guid> occurrenceIds = [.. occurrences.Select(o => o.Id)];
        if (occurrenceIds.Count == 0)
        {
            return;
        }

        Dictionary<Guid, SessionOccurrenceStats> stats = await dbContext.Set<SessionOccurrenceStats>()
            .AsNoTracking()
            .Where(stat => occurrenceIds.Contains(stat.SessionOccurrenceId))
            .ToDictionaryAsync(stat => stat.SessionOccurrenceId, cancellationToken);

        foreach (SessionOccurrence occurrence in occurrences)
        {
            occurrence.Stats = stats.TryGetValue(occurrence.Id, out SessionOccurrenceStats? stat)
                ? stat
                : new SessionOccurrenceStats { SessionOccurrenceId = occurrence.Id };
        }
    }
}
