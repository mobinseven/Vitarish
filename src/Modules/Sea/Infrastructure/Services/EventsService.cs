namespace Vitarish.Sea.Infrastructure.Services;

using System.Collections.Generic;
using System.Linq;
using Vitarish.Sea.Application.Abstractions;
using Vitarish.Sea.Domain.Dto;
using Vitarish.Sea.Domain.Entities;
using Vitarish.Sea.Domain.Models;
using Vitarish.Sea.Infrastructure.Extensions;

[RegisterScoped]
internal class EventsService(
    IServiceScopeFactory serviceScopeFactory,
    IMediator mediator,
    EventsMapper mapper,
    IAttachmentsService attachmentsService)
    : EntityService<Event, EventDescriptor, EventDto, EventWriteDto, EventDescriptorDto>(serviceScopeFactory, mapper.ToDto, mapper.ToDomain, mapper.ToDomain), IEventsService
{
    private readonly IMediator _mediator = mediator;

    public async Task<IReadOnlyList<EventDto>> GetByProcedureAsync(Guid procedureId, CancellationToken cancellationToken)
    {
        return (await GetListAsync(new EventDescriptor { ProcedureId = procedureId, IncludeStats = true }, cancellationToken))
            .EnsureValue("Get events by procedure");
    }

    public async Task UpdateChainAsync(
        Guid procedureId,
        IEnumerable<EventChainChangeDto> chainChangeDtos,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new UpdateEventsChainCommand(procedureId,
                [.. chainChangeDtos.Select(dto => new EventChainChange(dto.Id, dto.PreviousEventId))]),
            cancellationToken);
    }

    public async Task<EventDto?> AddAsync(
        Guid projectId,
        Guid procedureId,
        Guid? previousEventId,
        CancellationToken cancellationToken)
    {
        Event? entity = await _mediator.Send(new AddEventAfterCommand(projectId, procedureId, previousEventId), cancellationToken);

        return entity is null ? null : mapper.ToDto(entity);
    }

    public Task<bool> MoveUpAsync(Guid procedureId, Guid eventId, CancellationToken cancellationToken)
        => _mediator.Send(new MoveEventUpCommand(procedureId, eventId), cancellationToken);

    public Task<bool> MoveDownAsync(Guid procedureId, Guid eventId, CancellationToken cancellationToken)
        => _mediator.Send(new MoveEventDownCommand(procedureId, eventId), cancellationToken);

    public Task<bool> RemoveEventAsync(Guid procedureId, Guid eventId, CancellationToken cancellationToken)
        => _mediator.Send(new RemoveEventCommand(procedureId, eventId), cancellationToken);

    public Task RepairChainAsync(Guid procedureId, CancellationToken cancellationToken)
        => _mediator.Send(new RepairEventChainCommand(procedureId), cancellationToken);

    public async Task<EventDto?> UpdateAsync(
        Guid id,
        string title,
        string description,
        CancellationToken cancellationToken)
    {
        Event? en = (await GetEntityAsync(id, cancellationToken)).GetValueOrDefault();
        if(en is null)
        {
            return null;
        }

        en.Title = title;
        en.Description = description;
        return (await UpdateAsync(en, cancellationToken)).GetValueOrDefault();
    }

    public Task CommitAsync(Guid eventId, Guid reporterUserId, DateTimeOffset commitTime, CancellationToken cancellationToken)
        => _mediator.Send(new CommitEventCommand(eventId, reporterUserId, commitTime), cancellationToken);

    public Task<int> CommitAllByProcedureAsync(
        Guid procedureId,
        Guid reporterUserId,
        DateTimeOffset commitTime,
        CancellationToken cancellationToken)
        => _mediator.Send(new CommitProcedureEventsCommand(procedureId, reporterUserId, commitTime), cancellationToken);

    public Task UncommitAsync(Guid eventId, CancellationToken cancellationToken)
        => _mediator.Send(new UncommitEventCommand(eventId), cancellationToken);

    public Task<int> UncommitAllByProcedureAsync(Guid procedureId, CancellationToken cancellationToken)
        => _mediator.Send(new UncommitProcedureEventsCommand(procedureId), cancellationToken);

    public async Task<int> GetRemindersCountAsync(Guid eventId, CancellationToken cancellationToken)
    {
        return (await CountAsync<Reminder, ReminderDescriptor>(new() { EventId = eventId }, cancellationToken))
            .EnsureValue("Count event reminders");
    }

    public async Task<EventSnapshot?> CaptureSnapshotAsync(Guid id, CancellationToken cancellationToken)
    {
        EventDto? eventDto = (await GetAsync(id, cancellationToken)).GetValueOrDefault();
        if(eventDto is null)
        {
            return null;
        }

        IReadOnlyList<AttachmentDto> attachments = await attachmentsService.GetByEventAsync(id, cancellationToken);
        List<AttachmentSnapshot> attachmentSnapshots = new(attachments.Count);
        foreach(AttachmentDto attachment in attachments)
        {
            attachmentSnapshots.Add(new AttachmentSnapshot(attachment.Id, attachment.Content));
        }

        return new EventSnapshot(eventDto.Id, eventDto.Title, eventDto.IsCommitted, attachmentSnapshots);
    }
}
