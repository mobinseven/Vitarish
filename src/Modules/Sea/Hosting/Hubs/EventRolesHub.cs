using Vitarish.Entities.Server.Core;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Entities.Server.Services;
using Vitarish.Entities.Server.SignalR;
using Vitarish.Sea.Application.Abstractions;
using Vitarish.Sea.Domain.Dto;

namespace Vitarish.Sea.Hosting.Hubs;

public sealed class EventRolesHub : EntityServer<EventRoleDto, EventRoleDto, EventRoleDescriptorDto, IEventRolesService>
{
    private readonly IEventRolesService _service;

    public EventRolesHub(IServiceScopeFactory serviceScopeFactory, IEventRolesService service, IEntityUserService? entityUserService = null)
        : base(serviceScopeFactory, entityUserService)
    {
        _service = service;
    }

    public Task<(IReadOnlyList<Guid> ReadRoleIds, IReadOnlyList<Guid> WriteRoleIds)> GetIdsAsync(Guid eventId, CancellationToken cancellationToken) =>
        _service.GetIdsAsync(eventId, cancellationToken);

    public Task<IReadOnlyDictionary<Guid, (IReadOnlyList<Guid> ReadRoleIds, IReadOnlyList<Guid> WriteRoleIds)>> GetIdsForEventsAsync(IEnumerable<Guid> eventIds, CancellationToken cancellationToken) =>
        _service.GetIdsForEventsAsync(eventIds, cancellationToken);

    public Task<IReadOnlyList<Guid>> GetReadIdsAsync(Guid eventId, CancellationToken cancellationToken) =>
        _service.GetReadIdsAsync(eventId, cancellationToken);

    public Task<IReadOnlyList<Guid>> GetWriteIdsAsync(Guid eventId, CancellationToken cancellationToken) =>
        _service.GetWriteIdsAsync(eventId, cancellationToken);

    public Task AssignAsync(Guid eventId, Guid roleId, bool isWrite, CancellationToken cancellationToken) =>
        _service.AssignAsync(eventId, roleId, isWrite, cancellationToken);

    public Task UnassignAsync(Guid eventId, Guid roleId, bool isWrite, CancellationToken cancellationToken) =>
        _service.UnassignAsync(eventId, roleId, isWrite, cancellationToken);
}
