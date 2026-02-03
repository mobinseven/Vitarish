using Vitarish.Sea.Domain.Dto;

namespace Vitarish.Sea.Application.Abstractions;

public interface IEventRolesService : IEntityService<EventRoleDto, EventRoleWriteDto, EventRoleDescriptorDto>
{
    Task<(IReadOnlyList<Guid> ReadRoleIds, IReadOnlyList<Guid> WriteRoleIds)> GetIdsAsync(Guid eventId, CancellationToken cancellationToken);
    Task<IReadOnlyDictionary<Guid, (IReadOnlyList<Guid> ReadRoleIds, IReadOnlyList<Guid> WriteRoleIds)>> GetIdsForEventsAsync(IEnumerable<Guid> eventIds, CancellationToken cancellationToken);
    Task<IReadOnlyList<Guid>> GetReadIdsAsync(Guid eventId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Guid>> GetWriteIdsAsync(Guid eventId, CancellationToken cancellationToken);
    Task AssignAsync(Guid eventId, Guid roleId, bool isWrite, CancellationToken cancellationToken);
    Task UnassignAsync(Guid eventId, Guid roleId, bool isWrite, CancellationToken cancellationToken);
}
