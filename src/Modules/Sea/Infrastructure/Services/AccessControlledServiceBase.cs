using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Sea.Infrastructure.Services;

internal class AccessControlledServiceBase(
        IRequestContext requestContext,
        IEventRolesService eventRolesService,
        IUserRolesService userRolesService)
{
    protected Guid UserId;
    protected bool loaded = false;
    protected bool ElevatedAccess;
    protected async Task<bool> IsAdmin()
    {
        await EnsureInitialized();
        return ElevatedAccess;
    }
    protected HashSet<Guid> userReadRoleIds = [];
    protected HashSet<Guid> userWriteRoleIds = [];

    protected static Exception ElevatedAccessRequired => new InvalidOperationException("Elevated access is required to perform this operation");
    protected static Exception ReadRoleRequired => new InvalidOperationException("Read role is required to perform this operation");
    protected static Exception WriteRoleRequired => new InvalidOperationException("Write role is required to perform this operation");

    protected async Task EnsureInitialized()
    {
        if (loaded)
        {
            return;
        }
        if (Guid.TryParse(requestContext.UserId, out Guid id))
        {
            UserId = id;
            IReadOnlyList<UserRoleReadDto> userroles = await userRolesService.GetListAsync(new UserRoleDescriptorDto { UserId = id }, CancellationToken.None)
                .EnsureValueAsync("Load user roles");
            userReadRoleIds = [.. userroles.Where(r => r.Type == RoleType.Read).Select(r => r.RoleId)];
            userWriteRoleIds = [.. userroles.Where(r => r.Type == RoleType.Write).Select(r => r.RoleId)];
            userReadRoleIds = [.. userWriteRoleIds.Union(userReadRoleIds)];
            ElevatedAccess = requestContext.IsPlatformAdmin() || requestContext.IsTenantOwner();

            loaded = true;
        }
    }


    public async Task<bool> CanReadEvent(Guid eventId)
    {
        await EnsureInitialized();
        if (ElevatedAccess)
        {
            return true;
        }
        IReadOnlyList<Guid> rids = await eventRolesService.GetReadIdsAsync(eventId, CancellationToken.None);
        IReadOnlyList<Guid> wids = await eventRolesService.GetWriteIdsAsync(eventId, CancellationToken.None);

        return rids.Union(wids).Intersect(userReadRoleIds).Any();
    }
    public async Task<bool> CanWriteEvent(Guid eventId)
    {
        await EnsureInitialized();
        if (ElevatedAccess)
        {
            return true;
        }
        IReadOnlyList<Guid> ids = await eventRolesService.GetWriteIdsAsync(eventId, CancellationToken.None);
        return ids.Intersect(userWriteRoleIds).Any();
    }

    protected async Task<IReadOnlyDictionary<Guid, (bool CanRead, bool CanWrite)>> GetEventAccessAsync(IEnumerable<Guid> eventIds)
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

    protected async Task<IEnumerable<Guid>> FilterByReadRole(IEnumerable<Guid> eventIds)
    {
        await EnsureInitialized();
        IReadOnlyDictionary<Guid, (IReadOnlyList<Guid> ReadRoleIds, IReadOnlyList<Guid> WriteRoleIds)> rolesByEvent =
            await eventRolesService.GetIdsForEventsAsync(eventIds, CancellationToken.None);

        return rolesByEvent.Where(r => r.Value.ReadRoleIds.Intersect(userReadRoleIds).Any()).Select(r => r.Key);
    }

    protected async Task<IEnumerable<Guid>> FilterByWriteRole(IEnumerable<Guid> eventIds)
    {
        await EnsureInitialized();
        IReadOnlyDictionary<Guid, (IReadOnlyList<Guid> ReadRoleIds, IReadOnlyList<Guid> WriteRoleIds)> rolesByEvent =
            await eventRolesService.GetIdsForEventsAsync(eventIds, CancellationToken.None);

        return rolesByEvent.Where(r => r.Value.WriteRoleIds.Intersect(userWriteRoleIds).Any()).Select(r => r.Key);
    }
}
