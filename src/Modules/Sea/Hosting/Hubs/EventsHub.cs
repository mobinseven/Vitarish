using Vitarish.Entities.Server.Core;
using Vitarish.Entities.Server.Services;
using Vitarish.Entities.Server.SignalR;
using Vitarish.Sea.Application.Abstractions;
using Vitarish.Sea.Domain.Dto;

namespace Vitarish.Sea.Hosting.Hubs;

public sealed class EventsHub(IEventsService service, IEntityUserService? entityUserService = null)
    : EntityServer<EventDto, EventWriteDto, EventDescriptorDto, IEventsService>(service, entityUserService)
{
    public Task<IReadOnlyList<EventDto>> GetByProcedureAsync(Guid procedureId, CancellationToken cancellationToken) =>
        EntityService.GetByProcedureAsync(procedureId, cancellationToken);

    public Task<EventDto?> CreateEventAsync(Guid projectId, Guid procedureId, Guid? previousEventId, CancellationToken cancellationToken) =>
        EntityService.AddAsync(projectId, procedureId, previousEventId, cancellationToken);

    public Task<EventDto?> UpdateEventDetailsAsync(Guid id, string title, string description, CancellationToken cancellationToken) =>
        EntityService.UpdateAsync(id, title, description, cancellationToken);

    public Task<bool> MoveUpAsync(Guid procedureId, Guid eventId, CancellationToken cancellationToken) =>
        EntityService.MoveUpAsync(procedureId, eventId, cancellationToken);

    public Task<bool> MoveDownAsync(Guid procedureId, Guid eventId, CancellationToken cancellationToken) =>
        EntityService.MoveDownAsync(procedureId, eventId, cancellationToken);

    public Task<bool> RemoveEventAsync(Guid procedureId, Guid eventId, CancellationToken cancellationToken) =>
        EntityService.RemoveEventAsync(procedureId, eventId, cancellationToken);

    public Task RepairChainAsync(Guid procedureId, CancellationToken cancellationToken) =>
        EntityService.RepairChainAsync(procedureId, cancellationToken);

    public Task UpdateChainAsync(Guid procedureId, IEnumerable<EventChainChangeDto> chainChangeDtos, CancellationToken cancellationToken) =>
        EntityService.UpdateChainAsync(procedureId, chainChangeDtos, cancellationToken);

    public Task CommitAsync(Guid eventId, Guid reporterUserId, DateTimeOffset commitTime, CancellationToken cancellationToken) =>
        EntityService.CommitAsync(eventId, reporterUserId, commitTime, cancellationToken);

    public Task<int> CommitAllByProcedureAsync(Guid procedureId, Guid reporterUserId, DateTimeOffset commitTime, CancellationToken cancellationToken) =>
        EntityService.CommitAllByProcedureAsync(procedureId, reporterUserId, commitTime, cancellationToken);

    public Task UncommitAsync(Guid eventId, CancellationToken cancellationToken) =>
        EntityService.UncommitAsync(eventId, cancellationToken);

    public Task<int> UncommitAllByProcedureAsync(Guid procedureId, CancellationToken cancellationToken) =>
        EntityService.UncommitAllByProcedureAsync(procedureId, cancellationToken);

    public Task<int> GetRemindersCountAsync(Guid eventId, CancellationToken cancellationToken) =>
        EntityService.GetRemindersCountAsync(eventId, cancellationToken);

    public Task<EventSnapshot?> CaptureSnapshotAsync(Guid id, CancellationToken cancellationToken) =>
        EntityService.CaptureSnapshotAsync(id, cancellationToken);
}
