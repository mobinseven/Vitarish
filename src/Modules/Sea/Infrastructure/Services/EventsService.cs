namespace Vitarish.Sea.Infrastructure.Services;

[RegisterTransient]
internal class EventsService(
    IMediator mediator,
    IAttachmentsService attachmentsService)
    : EntityService<Event, EventDescriptor, EventReadDto, EventWriteDto, EventDescriptorDto>
    (
        mediator, evnt => evnt.ToDto(),
        evnt => evnt.ToDomain(),
        evnt => evnt.ToDomain()
    ),
    IEventsService
{

    public async Task<IReadOnlyList<EventReadDto>> GetByProcedureAsync(Guid procedureId, CancellationToken cancellationToken) => (await GetListAsync(new EventDescriptor { ProcedureId = procedureId, IncludeStats = true }, cancellationToken))
            .EnsureValue("Get events by procedure");

    public async Task UpdateChainAsync(
        Guid procedureId,
        IEnumerable<EventsChainChangeReadDto> chainChangeDtos,
        CancellationToken cancellationToken) => await Mediator.Send(
            new UpdateEventsChainCommand(procedureId,
                [.. chainChangeDtos.Select(dto => new EventChainChange(dto.Id, dto.PreviousEventId))]),
            cancellationToken);

    public async Task<EventReadDto?> AddAsync(
        Guid projectId,
        Guid procedureId,
        Guid? previousEventId,
        CancellationToken cancellationToken)
    {
        Event? entity = await Mediator.Send(new AddEventAfterCommand(projectId, procedureId, previousEventId), cancellationToken);

        return entity is null ? null : entity.ToDto();
    }

    public Task<bool> MoveUpAsync(Guid procedureId, Guid eventId, CancellationToken cancellationToken)
        => Mediator.Send(new MoveEventUpCommand(procedureId, eventId), cancellationToken);

    public Task<bool> MoveDownAsync(Guid procedureId, Guid eventId, CancellationToken cancellationToken)
        => Mediator.Send(new MoveEventDownCommand(procedureId, eventId), cancellationToken);

    public Task<bool> RemoveEventAsync(Guid procedureId, Guid eventId, CancellationToken cancellationToken)
        => Mediator.Send(new RemoveEventCommand(procedureId, eventId), cancellationToken);

    public Task RepairChainAsync(Guid procedureId, CancellationToken cancellationToken)
        => Mediator.Send(new RepairEventChainCommand(procedureId), cancellationToken);

    public async Task<EventReadDto?> UpdateAsync(
        Guid id,
        string title,
        string description,
        CancellationToken cancellationToken)
    {
        Event? en = (await GetEntityAsync(id, cancellationToken)).GetValueOrDefault();
        if (en is null)
        {
            return null;
        }

        en.Title = title;
        en.Description = description;
        return (await UpdateAsync(en, cancellationToken)).GetValueOrDefault();
    }

    public Task CommitAsync(Guid eventId, Guid reporterUserId, DateTimeOffset commitTime, CancellationToken cancellationToken)
        => Mediator.Send(new CommitEventCommand(eventId, reporterUserId, commitTime), cancellationToken);

    public Task<int> CommitAllByProcedureAsync(
        Guid procedureId,
        Guid reporterUserId,
        DateTimeOffset commitTime,
        CancellationToken cancellationToken)
        => Mediator.Send(new CommitProcedureEventsCommand(procedureId, reporterUserId, commitTime), cancellationToken);

    public Task UncommitAsync(Guid eventId, CancellationToken cancellationToken)
        => Mediator.Send(new UncommitEventCommand(eventId), cancellationToken);

    public Task<int> UncommitAllByProcedureAsync(Guid procedureId, CancellationToken cancellationToken)
        => Mediator.Send(new UncommitProcedureEventsCommand(procedureId), cancellationToken);

    public async Task<int> GetRemindersCountAsync(Guid eventId, CancellationToken cancellationToken) => (await CountAsync<Vitarish.Reminders.Domain.Entities.Reminder, Vitarish.Reminders.Domain.Models.ReminderDescriptor>(new() { SourceId = eventId }, cancellationToken))
            .EnsureValue("Count event reminders");

    public async Task<EventSnapshot?> CaptureSnapshotAsync(Guid id, CancellationToken cancellationToken)
    {
        EventReadDto? eventDto = (await GetAsync(id, cancellationToken)).GetValueOrDefault();
        if (eventDto is null)
        {
            return null;
        }

        IReadOnlyList<AttachmentReadDto> attachments = await attachmentsService.GetByEventAsync(id, cancellationToken);
        List<AttachmentSnapshot> attachmentSnapshots = new(attachments.Count);
        foreach (AttachmentReadDto attachment in attachments)
        {
            attachmentSnapshots.Add(new AttachmentSnapshot(attachment.Id, attachment.Content));
        }

        return new EventSnapshot(eventDto.Id, eventDto.Title, eventDto.IsCommitted, attachmentSnapshots);
    }
}
