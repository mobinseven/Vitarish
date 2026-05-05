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

    public new async Task<Event?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await Set.AsNoTracking()
            .Where(e => e.Id == id)
            .Select(e => new
            {
                Entity = e,
                Stats = dbContext.Set<EventStats>().FirstOrDefault(s => s.EventId == e.Id)
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (result is null)
        {
            return null;
        }

        result.Entity.Stats = result.Stats ?? new EventStats { EventId = id };
        return result.Entity;
    }

    public new async Task<IReadOnlyList<Event>> GetAsync(EventDescriptor descriptor, CancellationToken cancellationToken)
    {
        if (descriptor.IncludeStats == false)
        {
            return await base.GetAsync(descriptor, cancellationToken);
        }

        var results = await ApplyCore(Set.AsNoTracking(), descriptor)
            .Select(e => new
            {
                Entity = e,
                Stats = dbContext.Set<EventStats>().FirstOrDefault(s => s.EventId == e.Id)
            })
            .ToListAsync(cancellationToken);

        foreach (var result in results)
        {
            result.Entity.Stats = result.Stats ?? new EventStats { EventId = result.Entity.Id };
        }

        return results.ConvertAll(r => r.Entity);
    }

    Task<Event?> IEntityRepository<Event, EventDescriptor>.GetAsync(Guid id, CancellationToken cancellationToken)
        => GetAsync(id, cancellationToken);

    Task<IReadOnlyList<Event>> IEntityRepository<Event, EventDescriptor>.GetAsync(EventDescriptor descriptor, CancellationToken cancellationToken)
        => GetAsync(descriptor, cancellationToken);

    protected override bool IsDuplicateKeyException(DbUpdateException exception) => false;
}

