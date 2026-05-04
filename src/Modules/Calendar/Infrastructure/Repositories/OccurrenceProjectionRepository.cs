namespace Vitarish.Calendar.Infrastructure.Repositories;

[RegisterTransient]
internal class OccurrenceProjectionRepository(CalendarDbContext dbContext)
    : EntityRepository<OccurrenceProjection, OccurrenceProjectionDescriptor, CalendarDbContext>(dbContext),
      IEntityRepository<OccurrenceProjection, OccurrenceProjectionDescriptor>
{
    protected override IQueryable<OccurrenceProjection> Apply(IQueryable<OccurrenceProjection> query, OccurrenceProjectionDescriptor descriptor)
    {
        query = base.Apply(query, descriptor);

        if (descriptor.MappingId is { } mappingId)
        {
            query = query.Where(p => p.MappingId == mappingId);
        }

        if (descriptor.RangeStart is { } rangeStart)
        {
            query = query.Where(p => p.OccurrenceEnd >= rangeStart);
        }

        if (descriptor.RangeEnd is { } rangeEnd)
        {
            query = query.Where(p => p.OccurrenceStart <= rangeEnd);
        }

        if (descriptor.IsCancelled is { } isCancelled)
        {
            query = query.Where(p => p.IsCancelled == isCancelled);
        }

        return query;
    }

    protected override bool IsDuplicateKeyException(DbUpdateException exception) => false;

    /// <summary>
    /// Deletes all projections for a mapping that were projected before the given cutoff.
    /// Used to prune stale cache rows after a projection rebuild.
    /// </summary>
    public async Task DeleteByHorizonAsync(Guid mappingId, DateTimeOffset before, CancellationToken cancellationToken)
    {
        IReadOnlyList<OccurrenceProjection> stale = await GetAsync(
            new OccurrenceProjectionDescriptor { MappingId = mappingId },
            cancellationToken);

        IReadOnlyList<OccurrenceProjection> toDelete = [.. stale.Where(p => p.ProjectedAt < before)];

        if (toDelete.Count > 0)
        {
            await RemoveRangeAsync(toDelete, cancellationToken);
        }
    }
}
