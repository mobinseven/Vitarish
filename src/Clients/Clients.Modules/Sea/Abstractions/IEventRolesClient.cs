using Vitarish.Clients.Modules.Sea.Dto;
using Vitarish.Entities.Client.SignalR;

namespace Vitarish.Clients.Modules.Sea.Abstractions;

public interface IEventRolesClient : IEntityClient<EventRoleDto, EventRoleDto, EventRoleDescriptorDto>
{
    Task<(IReadOnlyList<Guid> ReadRoleIds, IReadOnlyList<Guid> WriteRoleIds)> GetIdsAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<Guid, (IReadOnlyList<Guid> ReadRoleIds, IReadOnlyList<Guid> WriteRoleIds)>> GetIdsForEventsAsync(IEnumerable<Guid> eventIds, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Guid>> GetReadIdsAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Guid>> GetWriteIdsAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task AssignAsync(Guid eventId, Guid roleId, bool isWrite, CancellationToken cancellationToken = default);
    Task UnassignAsync(Guid eventId, Guid roleId, bool isWrite, CancellationToken cancellationToken = default);
}
