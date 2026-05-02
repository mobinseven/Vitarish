namespace Vitarish.Sea.Infrastructure.Services;

[RegisterTransient]
internal class ProjectsService(
    IMediator mediator,
    IProceduresService proceduresService,
    SeaDbContext dbContext) : EntityService<Project, ProjectDescriptor, ProjectReadDto, ProjectWriteDto, ProjectDescriptorDto>
    (
        mediator,
        project => project.ToDto(),
        project => project.ToDomain(),
        project => project.ToDomain()
    ),
    IProjectsService
{
    public new async Task<EntityResult<ProjectReadDto>> AddAsync(ProjectWriteDto dto, CancellationToken cancellationToken)
        => await SyncUserIdsAfterWriteAsync(await base.AddAsync(dto, cancellationToken), dto, cancellationToken);

    public new async Task<EntityResult<ProjectReadDto>> UpdateAsync(ProjectWriteDto dto, CancellationToken cancellationToken)
        => await SyncUserIdsAfterWriteAsync(await base.UpdateAsync(dto, cancellationToken), dto, cancellationToken);

    private async Task<EntityResult<ProjectReadDto>> SyncUserIdsAfterWriteAsync(EntityResult<ProjectReadDto> result, ProjectWriteDto dto, CancellationToken cancellationToken)
    {
        if (result.IsSuccess && dto.UserIds is not null)
        {
            await SyncProjectUsersAsync(result.Value!.Id, dto.UserIds, cancellationToken);
            result.Value!.UserIds = [.. dto.UserIds];
        }
        return result;
    }

    private async Task SyncProjectUsersAsync(Guid projectId, IReadOnlyList<string> desiredUserIds, CancellationToken cancellationToken)
    {
        List<ProjectUser> existing = await dbContext.ProjectUsers
            .Where(pu => pu.ProjectId == projectId)
            .ToListAsync(cancellationToken);

        HashSet<string> desired = [.. desiredUserIds];
        HashSet<string> existingIds = [.. existing.Select(pu => pu.UserId)];

        foreach (ProjectUser pu in existing.Where(pu => !desired.Contains(pu.UserId)))
        {
            dbContext.ProjectUsers.Remove(pu);
        }

        foreach (string userId in desired.Where(id => !existingIds.Contains(id)))
        {
            await dbContext.ProjectUsers.AddAsync(new ProjectUser(projectId, userId), cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
