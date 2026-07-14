using Vitarish.Entities.Contracts.Models;
using Vitarish.Sea.Domain.Mappers;
using System.Text.Json;

namespace Vitarish.Sea.Infrastructure.Services;

[RegisterTransient]
internal class ProjectsService(
    IMediator mediator,
    IProceduresService proceduresService,
    IEventsService eventsService,
    SeaDbContext dbContext) : EntityService<Project, ProjectDescriptor, ProjectReadDto, ProjectWriteDto, ProjectDescriptorDto>
    (
        mediator,
        project => project.ToRead(),
        project => project.ToEntity(),
        project => project.ToDescriptor()
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
            _ = dbContext.ProjectUsers.Remove(pu);
        }

        foreach (string userId in desired.Where(id => !existingIds.Contains(id)))
        {
            _ = await dbContext.ProjectUsers.AddAsync(new ProjectUser(projectId, userId), cancellationToken);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<ProjectSnapshot?> CaptureSnapshotAsync(Guid id, CancellationToken cancellationToken)
    {
        ProjectReadDto? project = (await GetAsync(id, cancellationToken)).Value;
        if (project is null)
        {
            return null;
        }

        IReadOnlyList<ProcedureReadDto> procedures = (await proceduresService.GetListAsync(
            new ProcedureDescriptorDto { ProjectId = id }, cancellationToken)).Value ?? [];
        List<ProcedureSnapshot> procedureSnapshots = new(procedures.Count);
        foreach (ProcedureReadDto procedure in procedures)
        {
            IReadOnlyList<EventReadDto> events = await eventsService.GetByProcedureAsync(procedure.Id, cancellationToken);
            List<EventSnapshot> eventSnapshots = new(events.Count);
            foreach (EventReadDto @event in events)
            {
                EventSnapshot? snapshot = await eventsService.CaptureSnapshotAsync(@event.Id, cancellationToken);
                if (snapshot is not null)
                {
                    eventSnapshots.Add(snapshot);
                }
            }

            procedureSnapshots.Add(new ProcedureSnapshot(
                procedure.Id,
                project.Id,
                project.Name,
                project.Description,
                JsonSerializer.Serialize(project.Metadata),
                procedure.Name,
                JsonSerializer.Serialize(procedure.Metadata),
                null,
                null,
                procedure.Stats,
                eventSnapshots));
        }

        return new ProjectSnapshot(
            project.Id,
            project.Name,
            project.Description,
            project.Template,
            JsonSerializer.Serialize(project.Metadata),
            null,
            null,
            project.Stats,
            procedureSnapshots);
    }
}
