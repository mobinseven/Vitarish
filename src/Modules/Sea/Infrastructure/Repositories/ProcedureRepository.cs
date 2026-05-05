using System.Linq.Expressions;

namespace Vitarish.Sea.Infrastructure.Repositories;

[RegisterTransient]
internal class ProcedureRepository(SeaDbContext dbContext)
    : EntityRepository<Procedure, ProcedureDescriptor, SeaDbContext>(dbContext),
      IEntityRepository<Procedure, ProcedureDescriptor>
{
    protected override IQueryable<Procedure> Apply(IQueryable<Procedure> query, ProcedureDescriptor descriptor)
    {
        query = base.Apply(query, descriptor);

        if (descriptor.ProjectId is not null)
        {
            query = query.Where(p => p.ProjectId == descriptor.ProjectId);
        }

        if (!string.IsNullOrWhiteSpace(descriptor.Search))
        {
            IReadOnlyCollection<string> patterns = EntitySearchHelper.CreateSearchPatterns(descriptor.Search);

            if (patterns.Count > 0)
            {
                Expression<Func<Procedure, bool>> predicate = EntitySearchHelper.BuildSearchPredicate<Procedure>(
                    patterns,
                    procedure => procedure.Name);

                query = query.Where(predicate);
            }
        }

        if (descriptor.RoleIds?.Any() == true)
        {
            query = query.Where(p => dbContext.Events.Any(e => e.ProcedureId == p.Id
                && dbContext.EventRoles.Any(er => er.EventId == e.Id && descriptor.RoleIds!.Contains(er.RoleId))));
        }

        if (descriptor.IncludeStats == false)
        {
            query = query.IgnoreAutoIncludes();
        }

        return query;
    }

    public new async Task<Procedure?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await Set.AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new
            {
                Entity = p,
                Stats = dbContext.Set<ProcedureStats>().FirstOrDefault(s => s.ProcedureId == p.Id)
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (result is null)
        {
            return null;
        }

        result.Entity.Stats = result.Stats ?? new ProcedureStats { ProcedureId = id };
        return result.Entity;
    }

    public new async Task<IReadOnlyList<Procedure>> GetAsync(ProcedureDescriptor descriptor, CancellationToken cancellationToken)
    {
        if (descriptor.IncludeStats == false)
        {
            return await base.GetAsync(descriptor, cancellationToken);
        }

        var results = await ApplyCore(Set.AsNoTracking(), descriptor)
            .Select(p => new
            {
                Entity = p,
                Stats = dbContext.Set<ProcedureStats>().FirstOrDefault(s => s.ProcedureId == p.Id)
            })
            .ToListAsync(cancellationToken);

        foreach (var result in results)
        {
            result.Entity.Stats = result.Stats ?? new ProcedureStats { ProcedureId = result.Entity.Id };
        }

        return results.ConvertAll(r => r.Entity);
    }

    Task<Procedure?> IEntityRepository<Procedure, ProcedureDescriptor>.GetAsync(Guid id, CancellationToken cancellationToken)
        => GetAsync(id, cancellationToken);

    Task<IReadOnlyList<Procedure>> IEntityRepository<Procedure, ProcedureDescriptor>.GetAsync(ProcedureDescriptor descriptor, CancellationToken cancellationToken)
        => GetAsync(descriptor, cancellationToken);

    protected override bool IsDuplicateKeyException(DbUpdateException exception) => false;
}

