using Vitarish.Entities.Server.Core;
using Vitarish.Entities.Server.Services;
using Vitarish.Entities.Server.SignalR;
using Vitarish.Sea.Application.Abstractions;
using Vitarish.Sea.Domain.Dto;

namespace Vitarish.Sea.Hosting.Hubs;

public sealed class EventRolesHub(IEventRolesService service, IEntityUserService? entityUserService = null)
    : EntityServer<EventRoleDto, EventRoleWriteDto, EventRoleDescriptorDto, IEventRolesService>(service, entityUserService)
{
    public Task<(IReadOnlyList<Guid> ReadRoleIds, IReadOnlyList<Guid> WriteRoleIds)> GetIdsAsync(Guid eventId, CancellationToken cancellationToken) =>
        EntityService.GetIdsAsync(eventId, cancellationToken);

    public Task<IReadOnlyDictionary<Guid, (IReadOnlyList<Guid> ReadRoleIds, IReadOnlyList<Guid> WriteRoleIds)>> GetIdsForEventsAsync(IEnumerable<Guid> eventIds, CancellationToken cancellationToken) =>
        EntityService.GetIdsForEventsAsync(eventIds, cancellationToken);

    public Task<IReadOnlyList<Guid>> GetReadIdsAsync(Guid eventId, CancellationToken cancellationToken) =>
        EntityService.GetReadIdsAsync(eventId, cancellationToken);

    public Task<IReadOnlyList<Guid>> GetWriteIdsAsync(Guid eventId, CancellationToken cancellationToken) =>
        EntityService.GetWriteIdsAsync(eventId, cancellationToken);

    public Task AssignAsync(Guid eventId, Guid roleId, bool isWrite, CancellationToken cancellationToken) =>
        EntityService.AssignAsync(eventId, roleId, isWrite, cancellationToken);

    public Task UnassignAsync(Guid eventId, Guid roleId, bool isWrite, CancellationToken cancellationToken) =>
        EntityService.UnassignAsync(eventId, roleId, isWrite, cancellationToken);
}
