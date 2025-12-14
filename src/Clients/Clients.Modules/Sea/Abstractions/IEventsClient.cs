using Vitarish.Clients.Modules.Sea.Dto;

namespace Vitarish.Clients.Modules.Sea.Abstractions;

public interface IEventsClient
{
    Task<IReadOnlyList<EventDto>> GetByProcedureAsync(Guid procedureId, CancellationToken cancellationToken = default);
    Task<EventDto?> AddAsync(Guid projectId, Guid procedureId, Guid? previousEventId, CancellationToken cancellationToken = default);
    Task<EventDto?> UpdateAsync(Guid id, string title, string description, CancellationToken cancellationToken = default);
    Task<bool> MoveUpAsync(Guid procedureId, Guid eventId, CancellationToken cancellationToken = default);
    Task<bool> MoveDownAsync(Guid procedureId, Guid eventId, CancellationToken cancellationToken = default);
    Task<bool> RemoveEventAsync(Guid procedureId, Guid eventId, CancellationToken cancellationToken = default);
    Task RepairChainAsync(Guid procedureId, CancellationToken cancellationToken = default);
    Task UpdateChainAsync(Guid procedureId, IEnumerable<EventChainChangeDto> chainChangeDtos, CancellationToken cancellationToken = default);
    Task CommitAsync(Guid eventId, Guid reporterUserId, DateTimeOffset commitTime, CancellationToken cancellationToken = default);
    Task<int> CommitAllByProcedureAsync(Guid procedureId, Guid reporterUserId, DateTimeOffset commitTime, CancellationToken cancellationToken = default);
    Task UncommitAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<int> UncommitAllByProcedureAsync(Guid procedureId, CancellationToken cancellationToken = default);
    Task<int> GetRemindersCountAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<EventSnapshot?> CaptureSnapshotAsync(Guid id, CancellationToken cancellationToken = default);
}
