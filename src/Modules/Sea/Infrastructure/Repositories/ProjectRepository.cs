using System.Linq.Expressions;

namespace Vitarish.Sea.Infrastructure.Repositories;

[RegisterTransient]
internal class ProjectRepository(SeaDbContext dbContext)
    : EntityRepository<Project, ProjectDescriptor, SeaDbContext>(dbContext),
      IEntityRepository<Project, ProjectDescriptor>
{
    protected override IQueryable<Project> Apply(IQueryable<Project> query, ProjectDescriptor descriptor)
    {
        query = base.Apply(query, descriptor);

        if (!string.IsNullOrWhiteSpace(descriptor.Search))
        {
            IReadOnlyCollection<string> patterns = EntitySearchHelper.CreateSearchPatterns(descriptor.Search);

            if (patterns.Count > 0)
            {
                Expression<Func<Project, bool>> predicate = EntitySearchHelper.BuildSearchPredicate<Project>(
                    patterns,
                    project => project.Name,
                    project => project.Description);

                query = query.Where(predicate);
            }
        }

        if (descriptor.Template is not null)
        {
            query = query.Where(p => p.Template == descriptor.Template);
        }

        if (descriptor.RoleIds is not null)
        {
            query = query.Where(p => dbContext.Events
                .Any(ev => ev.ProjectId == p.Id &&
                           dbContext.EventRoles.Any(er => er.EventId == ev.Id && descriptor.RoleIds.Contains(er.RoleId))));
        }

        if (descriptor.UserIds is not null)
        {
            query = query.Where(p => dbContext.ProjectUsers
                .Any(pu => pu.ProjectId == p.Id && descriptor.UserIds.Contains(pu.UserId)));
        }

        if (descriptor.SortBy is { } sort)
        {
            query = (sort, descriptor.Descending) switch
            {
                (ProjectSortBy.Name, true) => query.OrderByDescending(p => p.Name),
                (ProjectSortBy.Name, false) => query.OrderBy(p => p.Name),
                (ProjectSortBy.Template, true) => query.OrderByDescending(p => p.Template),
                (ProjectSortBy.Template, false) => query.OrderBy(p => p.Template),
                _ => query
            };
        }

        return query;
    }

    public new async Task<Project?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await Set.AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new
            {
                Entity = p,
                Stats = dbContext.Set<ProjectStats>().FirstOrDefault(s => s.ProjectId == p.Id),
                UserIds = dbContext.ProjectUsers
                    .Where(pu => pu.ProjectId == p.Id)
                    .Select(pu => pu.UserId)
                    .ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (result is null)
        {
            return null;
        }

        result.Entity.Stats = result.Stats ?? new ProjectStats { ProjectId = id };
        result.Entity.UserIds = result.UserIds;
        return result.Entity;
    }

    public new async Task<IReadOnlyList<Project>> GetAsync(ProjectDescriptor descriptor, CancellationToken cancellationToken)
    {
        IQueryable<Project> query = ApplyCore(Set.AsNoTracking(), descriptor);
        bool includeStats = descriptor.IncludeStats != false;

        if (includeStats)
        {
            var results = await query
                .Select(p => new
                {
                    Entity = p,
                    Stats = dbContext.Set<ProjectStats>().FirstOrDefault(s => s.ProjectId == p.Id),
                    UserIds = dbContext.ProjectUsers
                        .Where(pu => pu.ProjectId == p.Id)
                        .Select(pu => pu.UserId)
                        .ToList()
                })
                .ToListAsync(cancellationToken);

            foreach (var result in results)
            {
                result.Entity.Stats = result.Stats ?? new ProjectStats { ProjectId = result.Entity.Id };
                result.Entity.UserIds = result.UserIds;
            }

            return results.ConvertAll(r => r.Entity);
        }
        else
        {
            var results = await query
                .Select(p => new
                {
                    Entity = p,
                    UserIds = dbContext.ProjectUsers
                        .Where(pu => pu.ProjectId == p.Id)
                        .Select(pu => pu.UserId)
                        .ToList()
                })
                .ToListAsync(cancellationToken);

            foreach (var result in results)
            {
                result.Entity.UserIds = result.UserIds;
            }

            return results.ConvertAll(r => r.Entity);
        }
    }

    Task<Project?> IEntityRepository<Project, ProjectDescriptor>.GetAsync(Guid id, CancellationToken cancellationToken)
        => GetAsync(id, cancellationToken);

    Task<IReadOnlyList<Project>> IEntityRepository<Project, ProjectDescriptor>.GetAsync(ProjectDescriptor descriptor, CancellationToken cancellationToken)
        => GetAsync(descriptor, cancellationToken);

    protected override bool IsDuplicateKeyException(DbUpdateException exception) => false;
}
