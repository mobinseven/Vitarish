namespace Vitarish.Sea.Infrastructure.Repositories;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Vitarish.Sea.Domain.Entities;
using Vitarish.Sea.Domain.Models;
using Vitarish.Sea.Infrastructure.Data;
using Vitarish.Sea.Infrastructure.Helpers;

[RegisterTransient]
internal class ProcedureRepository(SeaDbContext dbContext)
    : EntityRepository<Procedure, ProcedureDescriptor, SeaDbContext>(dbContext),
      IEntityRepository<Procedure, ProcedureDescriptor>
{
    protected override IQueryable<Procedure> Apply(IQueryable<Procedure> query, ProcedureDescriptor descriptor)
    {
        query = base.Apply(query, descriptor);

        if(descriptor.ProjectId is not null)
        {
            query = query.Where(p => p.ProjectId == descriptor.ProjectId);
        }

        if(!string.IsNullOrWhiteSpace(descriptor.Search))
        {
            IReadOnlyCollection<string> patterns = EntitySearchHelper.CreateSearchPatterns(descriptor.Search);

            if(patterns.Count > 0)
            {
                Expression<Func<Procedure, bool>> predicate = EntitySearchHelper.BuildSearchPredicate<Procedure>(
                    patterns,
                    procedure => procedure.Name,
                    procedure => procedure.Metadata);

                query = query.Where(predicate);
            }
        }

        if(descriptor.RoleIds?.Any() == true)
        {
            query = query.Where(p => dbContext.Events.Any(e => e.ProcedureId == p.Id
                && dbContext.EventRoles.Any(er => er.EventId == e.Id && descriptor.RoleIds!.Contains(er.RoleId))));
        }

        if(descriptor.IncludeStats == false)
        {
            query = query.IgnoreAutoIncludes();
        }

        return query;
    }

    private async Task PopulateStatsAsync(IReadOnlyList<Procedure> procedures, CancellationToken cancellationToken)
    {
        if(procedures.Count == 0)
        {
            return;
        }

        List<Guid> procedureIds = procedures
            .Select(procedure => procedure.Id)
            .ToList();

        Dictionary<Guid, ProcedureStats> stats = await dbContext.Set<ProcedureStats>()
            .AsNoTracking()
            .Where(stat => procedureIds.Contains(stat.ProcedureId))
            .ToDictionaryAsync(stat => stat.ProcedureId, cancellationToken);

        foreach(Procedure procedure in procedures)
        {
            procedure.Stats = stats.TryGetValue(procedure.Id, out ProcedureStats? stat)
                ? stat
                : new ProcedureStats { ProcedureId = procedure.Id, Procedure = procedure };
        }
    }

    private Task PopulateStatsAsync(Procedure procedure, CancellationToken cancellationToken)
        => PopulateStatsAsync(new[] { procedure }, cancellationToken);

    public new async Task<Procedure?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        Procedure? procedure = await base.GetAsync(id, cancellationToken);

        if(procedure is null)
        {
            return null;
        }

        await PopulateStatsAsync(procedure, cancellationToken);

        return procedure;
    }

    public new async Task<IReadOnlyList<Procedure>> GetAsync(ProcedureDescriptor descriptor, CancellationToken cancellationToken)
    {
        IReadOnlyList<Procedure> procedures = await base.GetAsync(descriptor, cancellationToken);

        if(procedures.Count == 0 || descriptor.IncludeStats == false)
        {
            return procedures;
        }

        await PopulateStatsAsync(procedures, cancellationToken);

        return procedures;
    }

    Task<Procedure?> IEntityRepository<Procedure, ProcedureDescriptor>.GetAsync(Guid id, CancellationToken cancellationToken)
        => GetAsync(id, cancellationToken);

    Task<IReadOnlyList<Procedure>> IEntityRepository<Procedure, ProcedureDescriptor>.GetAsync(ProcedureDescriptor descriptor, CancellationToken cancellationToken)
        => GetAsync(descriptor, cancellationToken);

    protected override bool IsDuplicateKeyException(DbUpdateException exception)
    {
        return false;
    }
}

