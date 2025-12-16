namespace Vitarish.Sea.Application.Abstractions;

using Vitarish.Sea.Domain.Dto;

public interface IEventsService : IEntityService<EventDto, EventDto, EventDescriptorDto>
{
    Task<IReadOnlyList<EventDto>> GetByProcedureAsync(Guid procedureId, CancellationToken cancellationToken);

    Task<EventDto?> AddAsync(
        Guid projectId,
        Guid procedureId,
        Guid? previousEventId,
        CancellationToken cancellationToken);

    Task<bool> MoveUpAsync(Guid procedureId, Guid eventId, CancellationToken cancellationToken);

    Task<bool> MoveDownAsync(Guid procedureId, Guid eventId, CancellationToken cancellationToken);

    Task<bool> RemoveEventAsync(Guid procedureId, Guid eventId, CancellationToken cancellationToken);

    Task RepairChainAsync(Guid procedureId, CancellationToken cancellationToken);

    Task<EventDto?> UpdateAsync(
        Guid Id,
        string Title,
        string Description,
        CancellationToken cancellationToken);

    Task UpdateChainAsync(
        Guid procedureId,
        IEnumerable<EventChainChangeDto> chainChangeDtos,
        CancellationToken cancellationToken);

    Task CommitAsync(Guid eventId, Guid reporterUserId, DateTimeOffset commitTime, CancellationToken cancellationToken);

    Task<int> CommitAllByProcedureAsync(
        Guid procedureId,
        Guid reporterUserId,
        DateTimeOffset commitTime,
        CancellationToken cancellationToken);

    Task UncommitAsync(Guid eventId, CancellationToken cancellationToken);

    Task<int> UncommitAllByProcedureAsync(Guid procedureId, CancellationToken cancellationToken);

    Task<int> GetRemindersCountAsync(Guid eventId, CancellationToken cancellationToken);

    Task<EventSnapshot?> CaptureSnapshotAsync(Guid id, CancellationToken cancellationToken);
}
