using Vitarish.Clients.Interface.Identity.Services;
using Vitarish.Clients.Interface.Sea.Abstractions;

namespace Vitarish.Clients.Interface.Sea.Internal;

public sealed class SeaUsersService(IEntityInterceptionProvider userService, IUserRolesClient userRolesService, IEventRolesClient eventRolesService, IRolesClient rolesService, IPersonalClientFactory userAccessClient) : UsersService(userAccessClient), ISeaUsersService
{
    private HashSet<Guid> userReadRoleIds = [];
    private HashSet<Guid> userWriteRoleIds = [];
    private IReadOnlyList<RoleDto> roles = [];

    public IReadOnlyList<Guid> RoleIds => userReadRoleIds.Union(userWriteRoleIds).ToList();
    public Guid? UserId => User is not null ? Guid.Parse(User.Id) : null;

    public override async Task Initialize(string[] roleNames)
    {
        await base.Initialize(roleNames);
        if (Guid.TryParse(User?.Id, out Guid id))
        {
            _ = userService.Use(new EntityInterceptionConfig { Time = DateTime.Now, UserId = id.ToString() });
            IReadOnlyList<UserRoleDto> userroles = await userRolesService.GetListAsync(new UserRoleDescriptorDto { UserId = id }, CancellationToken.None)
                .EnsureValueAsync("Load user roles");
            userReadRoleIds = [.. userroles.Where(r => r.Type == RoleType.Read).Select(r => r.RoleId)];
            userWriteRoleIds = [.. userroles.Where(r => r.Type == RoleType.Write).Select(r => r.RoleId)];
            userReadRoleIds = [.. userWriteRoleIds.Union(userReadRoleIds)];
            IReadOnlyList<RoleDto> allroles = await rolesService.GetListAsync(new RoleDescriptorDto(), CancellationToken.None)
                .EnsureValueAsync("Load roles");
            roles = [.. allroles.Where(role => userReadRoleIds.Union(userWriteRoleIds).Any(roleId => role.Id == roleId))];
        }
    }
    public async Task<bool> CanReadEvent(Guid eventId)
    {
        IReadOnlyList<Guid> ids = await eventRolesService.GetReadIdsAsync(eventId, CancellationToken.None);
        return ids.Intersect(userReadRoleIds).Any();
    }
    public async Task<bool> CanWriteEvent(Guid eventId)
    {
        IReadOnlyList<Guid> ids = await eventRolesService.GetWriteIdsAsync(eventId, CancellationToken.None);
        return ids.Intersect(userWriteRoleIds).Any();
    }

    public async Task<IReadOnlyDictionary<Guid, (bool CanRead, bool CanWrite)>> GetEventAccessAsync(IEnumerable<Guid> eventIds)
    {
        IReadOnlyDictionary<Guid, (IReadOnlyList<Guid> ReadRoleIds, IReadOnlyList<Guid> WriteRoleIds)> rolesByEvent =
            await eventRolesService.GetIdsForEventsAsync(eventIds, CancellationToken.None);

        return rolesByEvent.ToDictionary(
            pair => pair.Key,
            pair => (
                pair.Value.ReadRoleIds.Intersect(userReadRoleIds).Any(),
                pair.Value.WriteRoleIds.Intersect(userWriteRoleIds).Any()));
    }
}
