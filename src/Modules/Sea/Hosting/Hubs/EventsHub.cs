using Vorn.Entities;
using Vorn.Entities.Server.SignalR;
using Vitarish.Sea.Application.Abstractions;
using Vitarish.Sea.Domain.Dto;

namespace Vitarish.Sea.Hosting.Hubs;

public sealed class EventsHub : EntityServer<EventDto, EventDescriptorDto, IEventsService>
{
    private readonly IEventsService _service;

    public EventsHub(IEventsService service, IEntityUserService entityUserService)
        : base(service, entityUserService)
    {
        _service = service;
    }

    public Task<IReadOnlyList<EventDto>> GetByProcedureAsync(Guid procedureId, CancellationToken cancellationToken) =>
        _service.GetByProcedureAsync(procedureId, cancellationToken);

    public Task<EventDto?> CreateEventAsync(Guid projectId, Guid procedureId, Guid? previousEventId, CancellationToken cancellationToken) =>
        _service.AddAsync(projectId, procedureId, previousEventId, cancellationToken);

    public Task<EventDto?> UpdateEventDetailsAsync(Guid id, string title, string description, CancellationToken cancellationToken) =>
        _service.UpdateAsync(id, title, description, cancellationToken);

    public Task<bool> MoveUpAsync(Guid procedureId, Guid eventId, CancellationToken cancellationToken) =>
        _service.MoveUpAsync(procedureId, eventId, cancellationToken);

    public Task<bool> MoveDownAsync(Guid procedureId, Guid eventId, CancellationToken cancellationToken) =>
        _service.MoveDownAsync(procedureId, eventId, cancellationToken);

    public Task<bool> RemoveEventAsync(Guid procedureId, Guid eventId, CancellationToken cancellationToken) =>
        _service.RemoveEventAsync(procedureId, eventId, cancellationToken);

    public Task RepairChainAsync(Guid procedureId, CancellationToken cancellationToken) =>
        _service.RepairChainAsync(procedureId, cancellationToken);

    public Task UpdateChainAsync(Guid procedureId, IEnumerable<EventChainChangeDto> chainChangeDtos, CancellationToken cancellationToken) =>
        _service.UpdateChainAsync(procedureId, chainChangeDtos, cancellationToken);

    public Task CommitAsync(Guid eventId, Guid reporterUserId, DateTimeOffset commitTime, CancellationToken cancellationToken) =>
        _service.CommitAsync(eventId, reporterUserId, commitTime, cancellationToken);

    public Task<int> CommitAllByProcedureAsync(Guid procedureId, Guid reporterUserId, DateTimeOffset commitTime, CancellationToken cancellationToken) =>
        _service.CommitAllByProcedureAsync(procedureId, reporterUserId, commitTime, cancellationToken);

    public Task UncommitAsync(Guid eventId, CancellationToken cancellationToken) =>
        _service.UncommitAsync(eventId, cancellationToken);

    public Task<int> UncommitAllByProcedureAsync(Guid procedureId, CancellationToken cancellationToken) =>
        _service.UncommitAllByProcedureAsync(procedureId, cancellationToken);

    public Task<int> GetRemindersCountAsync(Guid eventId, CancellationToken cancellationToken) =>
        _service.GetRemindersCountAsync(eventId, cancellationToken);

    public Task<EventSnapshot?> CaptureSnapshotAsync(Guid id, CancellationToken cancellationToken) =>
        _service.CaptureSnapshotAsync(id, cancellationToken);
}
