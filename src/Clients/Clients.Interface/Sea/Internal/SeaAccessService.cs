
namespace Vitarish.Clients.Interface.Sea.Internal;


public interface ISeaAccessService : IAccessService
{
    IReadOnlyList<Guid> RoleIds { get; }
    Guid? UserId { get; }
    Task<bool> CanReadEvent(Guid eventId);
    Task<bool> CanWriteEvent(Guid eventId);
}

public sealed class SeaAccessService(IAccessService accessService, IEntityInterceptionProvider userService, IUserRolesClient userRolesService, IEventRolesClient eventRolesService, IRolesClient rolesService) : ISeaAccessService
{
    private HashSet<Guid> userReadRoleIds = [];
    private HashSet<Guid> userWriteRoleIds = [];
    private IReadOnlyList<RoleDto> roles = [];
    public Access Access => accessService.Access;
    public IEnumerable<string> Roles => accessService.Roles;
    public IReadOnlyList<UserDto> Users => accessService.Users;
    public UserDto? User => accessService.User;

    public IReadOnlyList<Guid> RoleIds => userReadRoleIds.Union(userWriteRoleIds).ToList();
    public Guid? UserId => accessService.User is not null ? Guid.Parse(accessService.User.Id) : null;

    public async Task InitializeAsync(string? clientId = null)
    {
        await accessService.InitializeAsync(clientId);
        if (Guid.TryParse(accessService.User?.Id, out Guid id))
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
    public void RenameRole(string roleName, string roleNewName) => accessService.RenameRole(roleName, roleNewName);

    public Task<IEnumerable<ClaimDto>> GetUserClaimsAsync(string userId, CancellationToken ct) => accessService.GetUserClaimsAsync(userId, ct);

    public Task<IEnumerable<ClaimDto>> GetUserAccessClaimsAsync(string userId, string claimPrefix, CancellationToken ct) => accessService.GetUserAccessClaimsAsync(userId, claimPrefix, ct);

    public Task<ClaimDto?> GetUserAccessClaimAsync(string userId, string clientId, CancellationToken ct) => accessService.GetUserAccessClaimAsync(userId, clientId, ct);

    public Task<bool> HasUserAccessAsync(string userId, string clientId, CancellationToken ct) => accessService.HasUserAccessAsync(userId, clientId, ct);

    public Task<IEnumerable<UserDto>> GetClientUsersAsync(string clientId, CancellationToken ct) => accessService.GetClientUsersAsync(clientId, ct);

    public Task<UserDto?> GetClientUserAsync(string clientId, string userId, CancellationToken ct) => accessService.GetClientUserAsync(clientId, userId, ct);

    public Task SetUserAccessAsync(string userId, string clientId, string accessValue, CancellationToken ct) => accessService.SetUserAccessAsync(userId, clientId, accessValue, ct);

    public Task RemoveUserAccessAsync(string userId, string clientId, CancellationToken ct) => accessService.RemoveUserAccessAsync(userId, clientId, ct);
}

