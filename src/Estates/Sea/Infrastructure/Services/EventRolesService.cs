using Vitarish.Sea.Application.Commands.Roles;

namespace Vitarish.Sea.Infrastructure.Services;

[RegisterTransient]
internal class EventRolesService(IMediator mediator, EventRolesMapper mapper)
    : EntityService<EventRole, EventRoleDescriptor, EventRoleReadDto, EventRoleWriteDto, EventRoleDescriptorDto>(mediator, mapper.ToDto, mapper.ToDomain, mapper.ToDomain), IEventRolesService
{
    public async Task<(IReadOnlyList<Guid> ReadRoleIds, IReadOnlyList<Guid> WriteRoleIds)> GetIdsAsync(Guid eventId, CancellationToken cancellationToken)
    {
        IReadOnlyList<EventRoleReadDto> roles = (await GetListAsync(new EventRoleDescriptorDto { EventId = eventId }, cancellationToken))
            .EnsureValue("Get event roles");
        List<Guid> read = roles.Where(r => r.Type == RoleType.Read).Select(r => r.RoleId).ToList();
        List<Guid> write = roles.Where(r => r.Type == RoleType.Write).Select(r => r.RoleId).ToList();
        return (read, write);
    }

    public async Task<IReadOnlyDictionary<Guid, (IReadOnlyList<Guid> ReadRoleIds, IReadOnlyList<Guid> WriteRoleIds)>> GetIdsForEventsAsync(IEnumerable<Guid> eventIds, CancellationToken cancellationToken)
    {
        Guid[] ids = eventIds.Distinct().ToArray();
        if (ids.Length == 0)
        {
            return new Dictionary<Guid, (IReadOnlyList<Guid>, IReadOnlyList<Guid>)>();
        }

        IReadOnlyList<EventRoleReadDto> roles = (await GetListAsync(new EventRoleDescriptorDto(), cancellationToken))
            .EnsureValue("Get event roles");

        Dictionary<Guid, (IReadOnlyList<Guid> ReadRoleIds, IReadOnlyList<Guid> WriteRoleIds)> result = roles
            .Where(role => ids.Contains(role.EventId))
            .GroupBy(role => role.EventId)
            .ToDictionary(
                group => group.Key,
                group => (
                    (IReadOnlyList<Guid>)group.Where(role => role.Type == RoleType.Read).Select(role => role.RoleId).ToList(),
                    (IReadOnlyList<Guid>)group.Where(role => role.Type == RoleType.Write).Select(role => role.RoleId).ToList()));

        foreach (Guid id in ids)
        {
            _ = result.TryAdd(id, (Array.Empty<Guid>(), Array.Empty<Guid>()));
        }

        return result;
    }

    public async Task<IReadOnlyList<Guid>> GetReadIdsAsync(Guid eventId, CancellationToken cancellationToken)
    {
        IReadOnlyList<EventRoleReadDto> roles = (await GetListAsync(new EventRoleDescriptorDto { EventId = eventId, Type = RoleType.Read }, cancellationToken))
            .EnsureValue("Get read event roles");
        return [.. roles.Select(r => r.RoleId)];
    }

    public async Task<IReadOnlyList<Guid>> GetWriteIdsAsync(Guid eventId, CancellationToken cancellationToken)
    {
        IReadOnlyList<EventRoleReadDto> roles = (await GetListAsync(new EventRoleDescriptorDto { EventId = eventId, Type = RoleType.Write }, cancellationToken))
            .EnsureValue("Get write event roles");
        return [.. roles.Select(r => r.RoleId)];
    }

    public Task AssignAsync(Guid eventId, Guid roleId, bool isWrite, CancellationToken cancellationToken) => Mediator.Send(new AddRoleToEventCommand(eventId, roleId, isWrite), cancellationToken);

    public Task UnassignAsync(Guid eventId, Guid roleId, bool isWrite, CancellationToken cancellationToken) => Mediator.Send(new RemoveRoleFromEventCommand(eventId, roleId, isWrite), cancellationToken);
}
