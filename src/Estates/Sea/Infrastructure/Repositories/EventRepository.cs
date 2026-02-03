using System.Linq.Expressions;

namespace Vitarish.Sea.Infrastructure.Repositories;

[RegisterTransient]
internal class EventRepository(SeaDbContext dbContext)
    : EntityRepository<Event, EventDescriptor, SeaDbContext>(dbContext),
      IEntityRepository<Event, EventDescriptor>
{
    protected override IQueryable<Event> Apply(IQueryable<Event> query, EventDescriptor descriptor)
    {
        query = base.Apply(query, descriptor);

        if (descriptor.ProjectId is not null)
        {
            query = query.Where(e => e.ProjectId == descriptor.ProjectId);
        }

        if (descriptor.ProcedureId is not null)
        {
            query = query.Where(e => e.ProcedureId == descriptor.ProcedureId);
        }

        if (!string.IsNullOrWhiteSpace(descriptor.Search))
        {
            IReadOnlyCollection<string> patterns = EntitySearchHelper.CreateSearchPatterns(descriptor.Search);

            if (patterns.Count > 0)
            {
                Expression<Func<Event, bool>> predicate = EntitySearchHelper.BuildSearchPredicate<Event>(
                    patterns,
                    e => e.Title,
                    e => e.Description);

                query = query.Where(predicate);
            }
        }

        if (descriptor.PreviousEventId is not null)
        {
            query = query.Where(e => e.PreviousEventId == descriptor.PreviousEventId);
        }

        if (descriptor.CommittedBy is not null)
        {
            query = query.Where(e => e.CommittedBy == descriptor.CommittedBy);
        }

        if (descriptor.CommittedFrom is not null)
        {
            query = query.Where(e => e.CommittedAt >= descriptor.CommittedFrom);
        }

        if (descriptor.CommittedTo is not null)
        {
            query = query.Where(e => e.CommittedAt <= descriptor.CommittedTo);
        }

        if (descriptor.IsCommitted is not null)
        {
            query = query.Where(e => e.CommittedAt != null == descriptor.IsCommitted);
        }

        if (descriptor.ReadRoleId is not null)
        {
            Guid roleId = descriptor.ReadRoleId.Value;
            query = query.Where(e => dbContext.EventRoles.Any(er => er.EventId == e.Id && er.RoleId == roleId && er.Type == RoleType.Read));
        }

        if (descriptor.WriteRoleId is not null)
        {
            Guid roleId = descriptor.WriteRoleId.Value;
            query = query.Where(e => dbContext.EventRoles.Any(er => er.EventId == e.Id && er.RoleId == roleId && er.Type == RoleType.Write));
        }

        if (descriptor.IncludeStats == false)
        {
            query = query.IgnoreAutoIncludes();
        }

        return query;
    }

    private async Task PopulateStatsAsync(IReadOnlyList<Event> events, CancellationToken cancellationToken)
    {
        if (events.Count == 0)
        {
            return;
        }

        List<Guid> eventIds = [.. events.Select(@event => @event.Id)];

        Dictionary<Guid, EventStats> stats = await dbContext.Set<EventStats>()
            .AsNoTracking()
            .Where(stat => eventIds.Contains(stat.EventId))
            .ToDictionaryAsync(stat => stat.EventId, cancellationToken);

        foreach (Event @event in events)
        {
            @event.Stats = stats.TryGetValue(@event.Id, out EventStats? stat)
                ? stat
                : new EventStats { EventId = @event.Id };
        }
    }

    private Task PopulateStatsAsync(Event @event, CancellationToken cancellationToken)
        => PopulateStatsAsync([@event], cancellationToken);

    public new async Task<Event?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        Event? @event = await base.GetAsync(id, cancellationToken);

        if (@event is null)
        {
            return null;
        }

        await PopulateStatsAsync(@event, cancellationToken);

        return @event;
    }

    public new async Task<IReadOnlyList<Event>> GetAsync(EventDescriptor descriptor, CancellationToken cancellationToken)
    {
        IReadOnlyList<Event> events = await base.GetAsync(descriptor, cancellationToken);

        if (events.Count == 0 || descriptor.IncludeStats == false)
        {
            return events;
        }

        await PopulateStatsAsync(events, cancellationToken);

        return events;
    }

    Task<Event?> IEntityRepository<Event, EventDescriptor>.GetAsync(Guid id, CancellationToken cancellationToken)
        => GetAsync(id, cancellationToken);

    Task<IReadOnlyList<Event>> IEntityRepository<Event, EventDescriptor>.GetAsync(EventDescriptor descriptor, CancellationToken cancellationToken)
        => GetAsync(descriptor, cancellationToken);

    protected override bool IsDuplicateKeyException(DbUpdateException exception) => false;
}

