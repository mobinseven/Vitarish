namespace Vitarish.Sea.Infrastructure.Services;

[RegisterTransient]
internal class ProceduresService(
    IMediator mediator,
    IEventsService eventsService)
    : EntityService<Procedure, ProcedureDescriptor, ProcedureReadDto, ProcedureWriteDto, ProcedureDescriptorDto>
    (
        mediator,
        procedure=>procedure.ToDto(),
        procedure => procedure.ToDomain(),
        procedure => procedure.ToDomain()
    ),
    IProceduresService
{
    public async Task<Guid> AddAsync(Guid projectId, string name, CancellationToken cancellationToken)
    {
        Procedure entity = new(projectId, name);
        _ = (await AddEntityAsync(entity, cancellationToken)).EnsureValue("Add procedure");
        return entity.Id;
    }

    public async Task UpdateAsync(Guid id, string name, CancellationToken cancellationToken)
    {
        Procedure? entity = (await GetEntityAsync(id, cancellationToken)).GetValueOrDefault();
        if (entity is null)
        {
            return;
        }

        entity.Rename(name);
        _ = (await UpdateEntityAsync(entity, cancellationToken)).EnsureValue("Update procedure");
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        Procedure? entity = (await GetEntityAsync(id, cancellationToken)).GetValueOrDefault();
        if (entity is null)
        {
            return;
        }

        await Mediator.Send(new UpdateEventsChainCommand(id, []), cancellationToken);
        (await RemoveAsync(entity, cancellationToken)).EnsureSuccess("Remove procedure");
    }

    public async Task UpdateMetadataAsync(Guid id, string metadata, CancellationToken cancellationToken)
    {
        Procedure? entity = (await GetEntityAsync(id, cancellationToken)).GetValueOrDefault();
        if (entity is null)
        {
            return;
        }

        entity.SetMetadata(metadata);
        _ = (await UpdateEntityAsync(entity, cancellationToken)).EnsureValue("Update procedure metadata");
    }

    public async Task<ProcedureSnapshot?> CaptureSnapshotAsync(Guid id, CancellationToken cancellationToken)
    {
        ProcedureReadDto? procedure = (await GetAsync(id, cancellationToken)).GetValueOrDefault();
        if (procedure is null)
        {
            return null;
        }

        (string? overview, DateTimeOffset? updatedAt) = OverviewMetadataParser.Parse(procedure.Metadata);

        IReadOnlyList<EventReadDto> events = await eventsService.GetByProcedureAsync(id, cancellationToken);
        List<EventSnapshot> eventSnapshots = new(events.Count);
        foreach (EventReadDto eventDto in events)
        {
            EventSnapshot? snapshot = await eventsService.CaptureSnapshotAsync(eventDto.Id, cancellationToken);
            if (snapshot is not null)
            {
                eventSnapshots.Add(snapshot);
            }
        }

        return new ProcedureSnapshot(
            procedure.Id,
            procedure.ProjectId,
            string.Empty,
            string.Empty,
            "{}",
            procedure.Name,
            procedure.Metadata,
            overview,
            updatedAt,
            procedure.Stats,
            eventSnapshots);
    }
}
