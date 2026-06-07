using System.Linq.Expressions;

namespace Vitarish.Sea.Infrastructure.Repositories;

[RegisterTransient]
internal class ProjectRepository(
    SeaDbContext dbContext,
    EntitySearchPatternFactory searchPatternFactory,
    EntitySearchPredicateBuilder searchPredicateBuilder)
    : EntityRepository<Project, ProjectDescriptor, SeaDbContext>(dbContext),
      IEntityRepository<Project, ProjectDescriptor>
{
    protected override IQueryable<Project> Apply(IQueryable<Project> query, ProjectDescriptor descriptor)
    {
        query = base.Apply(query, descriptor);

        if (!string.IsNullOrWhiteSpace(descriptor.Search))
        {
            IReadOnlyCollection<string> patterns = searchPatternFactory.CreateSearchPatterns(descriptor.Search);

            if (patterns.Count > 0)
            {
                Expression<Func<Project, bool>> predicate = searchPredicateBuilder.BuildSearchPredicate<Project>(
                    patterns,
                    project => project.Name,
                    project => project.Description,
                    project => project.IdentificationCode);

                query = query.Where(predicate);
            }
        }

        if (descriptor.Template is not null)
        {
            query = query.Where(p => p.Template == descriptor.Template);
        }

        if (!string.IsNullOrWhiteSpace(descriptor.IdentificationCode))
        {
            string code = descriptor.IdentificationCode.Trim();
            query = query.Where(p => p.IdentificationCode == code);
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

    private async Task PopulateStatsAsync(IReadOnlyList<Project> projects, CancellationToken cancellationToken)
    {
        if (projects.Count == 0)
        {
            return;
        }

        List<Guid> projectIds = [.. projects.Select(project => project.Id)];

        Dictionary<Guid, ProjectStats> stats = await dbContext.Set<ProjectStats>()
            .AsNoTracking()
            .Where(stat => projectIds.Contains(stat.ProjectId))
            .ToDictionaryAsync(stat => stat.ProjectId, cancellationToken);

        foreach (Project project in projects)
        {
            project.Stats = stats.TryGetValue(project.Id, out ProjectStats? stat)
                ? stat
                : new ProjectStats { ProjectId = project.Id };
        }
    }

    private Task PopulateStatsAsync(Project project, CancellationToken cancellationToken)
        => PopulateStatsAsync([project], cancellationToken);

    private async Task PopulateUserIdsAsync(IReadOnlyList<Project> projects, CancellationToken cancellationToken)
    {
        if (projects.Count == 0)
        {
            return;
        }

        List<Guid> projectIds = [.. projects.Select(p => p.Id)];

        ILookup<Guid, string> userIdsByProject = (await dbContext.ProjectUsers
            .AsNoTracking()
            .Where(pu => projectIds.Contains(pu.ProjectId))
            .Select(pu => new { pu.ProjectId, pu.UserId })
            .ToListAsync(cancellationToken))
            .ToLookup(pu => pu.ProjectId, pu => pu.UserId);

        foreach (Project project in projects)
        {
            project.UserIds = [.. userIdsByProject[project.Id]];
        }
    }

    private Task PopulateUserIdsAsync(Project project, CancellationToken cancellationToken)
        => PopulateUserIdsAsync([project], cancellationToken);

    public new async Task<Project?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        Project? project = await base.GetAsync(id, cancellationToken);

        if (project is null)
        {
            return null;
        }

        await PopulateStatsAsync(project, cancellationToken);
        await PopulateUserIdsAsync(project, cancellationToken);

        return project;
    }

    public new async Task<IReadOnlyList<Project>> GetAsync(ProjectDescriptor descriptor, CancellationToken cancellationToken)
    {
        IReadOnlyList<Project> projects = await base.GetAsync(descriptor, cancellationToken);

        if (projects.Count == 0)
        {
            return projects;
        }

        if (descriptor.IncludeStats != false)
        {
            await PopulateStatsAsync(projects, cancellationToken);
        }

        await PopulateUserIdsAsync(projects, cancellationToken);

        return projects;
    }

    Task<Project?> IEntityRepository<Project, ProjectDescriptor>.GetAsync(Guid id, CancellationToken cancellationToken)
        => GetAsync(id, cancellationToken);

    Task<IReadOnlyList<Project>> IEntityRepository<Project, ProjectDescriptor>.GetAsync(ProjectDescriptor descriptor, CancellationToken cancellationToken)
        => GetAsync(descriptor, cancellationToken);

    protected override bool IsDuplicateKeyException(DbUpdateException exception) => false;
}
