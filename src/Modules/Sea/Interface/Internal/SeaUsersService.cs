using Duende.IdentityServer.Extensions;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Sea.Interface.Internal;

public sealed class SeaUsersService(
    IRequestContext requestContext,
    IUserRolesService userRolesService,
    IEventRolesService eventRolesService) : ISeaUsersService
{
    private bool loaded = false;
    private HashSet<Guid> userReadRoleIds = [];
    private HashSet<Guid> userWriteRoleIds = [];

    public async Task<IReadOnlyList<Guid>> GetRoleIds()
    {
        await EnsureInitialized();
        return [.. userReadRoleIds.Union(userWriteRoleIds)];
    }
    private async Task EnsureInitialized()
    {
        if (loaded)
        {
            return;
        }
        if (Guid.TryParse(requestContext.UserId, out Guid id))
        {
            IReadOnlyList<UserRoleDto> userroles = await userRolesService.GetListAsync(new UserRoleDescriptorDto { UserId = id }, CancellationToken.None)
                .EnsureValueAsync("Load user roles");
            userReadRoleIds = [.. userroles.Where(r => r.Type == RoleType.Read).Select(r => r.RoleId)];
            userWriteRoleIds = [.. userroles.Where(r => r.Type == RoleType.Write).Select(r => r.RoleId)];
            userReadRoleIds = [.. userWriteRoleIds.Union(userReadRoleIds)];
            loaded = true;
        }
    }
    public async Task<bool> CanReadEvent(Guid eventId)
    {
        await EnsureInitialized();
        IReadOnlyList<Guid> ids = await eventRolesService.GetReadIdsAsync(eventId, CancellationToken.None);
        return ids.Intersect(userReadRoleIds).Any();
    }
    public async Task<bool> CanWriteEvent(Guid eventId)
    {
        await EnsureInitialized();
        IReadOnlyList<Guid> ids = await eventRolesService.GetWriteIdsAsync(eventId, CancellationToken.None);
        return ids.Intersect(userWriteRoleIds).Any();
    }

    public async Task<IReadOnlyDictionary<Guid, (bool CanRead, bool CanWrite)>> GetEventAccessAsync(IEnumerable<Guid> eventIds)
    {
        await EnsureInitialized();
        IReadOnlyDictionary<Guid, (IReadOnlyList<Guid> ReadRoleIds, IReadOnlyList<Guid> WriteRoleIds)> rolesByEvent =
            await eventRolesService.GetIdsForEventsAsync(eventIds, CancellationToken.None);

        return rolesByEvent.ToDictionary(
            pair => pair.Key,
            pair => (
                pair.Value.ReadRoleIds.Intersect(userReadRoleIds).Any(),
                pair.Value.WriteRoleIds.Intersect(userWriteRoleIds).Any()));
    }
}
