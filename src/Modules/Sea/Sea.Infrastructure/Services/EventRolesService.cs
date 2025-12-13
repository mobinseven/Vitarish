using Vitarish.Sea.Application.Abstractions;
using Vitarish.Sea.Infrastructure.Extensions;
using Vitarish.Sea.Application.Commands.Roles;

namespace Vitarish.Sea.Infrastructure.Services;

[RegisterScoped]
internal class EventRolesService(IMediator mediator, EventRolesMapper mapper)
    : EntityService<EventRole, EventRoleDescriptor, EventRoleDto, EventRoleDescriptorDto>(mediator, mapper.ToDto, mapper.ToDomain, mapper.ToDomain), IEventRolesService
{
    public async Task<(IReadOnlyList<Guid> ReadRoleIds, IReadOnlyList<Guid> WriteRoleIds)> GetIdsAsync(Guid eventId, CancellationToken cancellationToken)
    {
        IReadOnlyList<EventRoleDto> roles = (await GetListAsync(new EventRoleDescriptorDto { EventId = eventId }, cancellationToken))
            .EnsureValue("Get event roles");
        List<Guid> read = roles.Where(r => r.Type == RoleType.Read).Select(r => r.RoleId).ToList();
        List<Guid> write = roles.Where(r => r.Type == RoleType.Write).Select(r => r.RoleId).ToList();
        return (read, write);
    }

    public async Task<IReadOnlyList<Guid>> GetReadIdsAsync(Guid eventId, CancellationToken cancellationToken)
    {
        IReadOnlyList<EventRoleDto> roles = (await GetListAsync(new EventRoleDescriptorDto { EventId = eventId, Type = RoleType.Read }, cancellationToken))
            .EnsureValue("Get read event roles");
        return [.. roles.Select(r => r.RoleId)];
    }

    public async Task<IReadOnlyList<Guid>> GetWriteIdsAsync(Guid eventId, CancellationToken cancellationToken)
    {
        IReadOnlyList<EventRoleDto> roles = (await GetListAsync(new EventRoleDescriptorDto { EventId = eventId, Type = RoleType.Write }, cancellationToken))
            .EnsureValue("Get write event roles");
        return [.. roles.Select(r => r.RoleId)];
    }

    public Task AssignAsync(Guid eventId, Guid roleId, bool isWrite, CancellationToken cancellationToken)
    {
        return Mediator.Send(new AddRoleToEventCommand(eventId, roleId, isWrite), cancellationToken);
    }

    public Task UnassignAsync(Guid eventId, Guid roleId, bool isWrite, CancellationToken cancellationToken)
    {
        return Mediator.Send(new RemoveRoleFromEventCommand(eventId, roleId, isWrite), cancellationToken);
    }
}
