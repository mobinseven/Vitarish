namespace Vitarish.Clients.Interface.Sea.Components.Users;

public partial class UserRolesSelect : UsersBase
{
    [Parameter, EditorRequired] public UserDto User { get; set; }

    private List<RoleDto> roles = [];
    private HashSet<Guid> readRoles = [];
    private HashSet<Guid> writeRoles = [];
    private HashSet<Guid> initialReadRoles = [];
    private HashSet<Guid> initialWriteRoles = [];
    private bool? selectAllRead = false;
    private bool? selectAllWrite = false;

    protected override async Task OnInitializedAsync()
    {
        if (!TryGetUserId(out Guid userId))
        {
            return;
        }

        roles = [.. await RolesService.GetListAsync(new RoleDescriptorDto(), CancellationToken.None)
            .EnsureValueAsync("Load roles")];
        IReadOnlyList<UserRoleDto> userRoles = await UserRolesService.GetListAsync(
                new UserRoleDescriptorDto { UserId = userId },
                CancellationToken.None)
            .EnsureValueAsync("Load user roles");
        readRoles = [.. userRoles.Where(r => r.Type == RoleType.Read).Select(r => r.RoleId)];
        writeRoles = [.. userRoles.Where(r => r.Type == RoleType.Write).Select(r => r.RoleId)];
        initialReadRoles = [.. readRoles];
        initialWriteRoles = [.. writeRoles];
        UpdateSelectAllStates();
    }

    private async Task SaveAsync()
    {
        if (!TryGetUserId(out Guid userId))
        {
            return;
        }

        IEnumerable<Guid> allRoleIds = initialReadRoles
            .Union(initialWriteRoles)
            .Union(readRoles)
            .Union(writeRoles)
            .Where(id => initialReadRoles.Contains(id) != readRoles.Contains(id) || initialWriteRoles.Contains(id) != writeRoles.Contains(id));

        IEnumerable<Task> updateTasks = allRoleIds.Select(async id =>
        {
            await UserRolesService.RemoveUserRoleAsync(userId, id, CancellationToken.None);

            if (readRoles.Contains(id))
            {
                await UserRolesService.AssignAsync(userId, id, RoleType.Read, CancellationToken.None);
            }

            if (writeRoles.Contains(id))
            {
                await UserRolesService.AssignAsync(userId, id, RoleType.Write, CancellationToken.None);
            }
        });

        await Task.WhenAll(updateTasks);
    }

    private bool TryGetUserId(out Guid userId)
    {
        if (!Guid.TryParse(User.Id, out userId))
        {
            // TODO: Log this error and notify the user.
            return false;
        }

        return true;
    }

    private bool IsAssigned(Guid id, RoleType type)
        => type == RoleType.Read ? readRoles.Contains(id) : writeRoles.Contains(id);

    private void OnRoleChanged(Guid id, RoleType type, bool value)
    {
        HashSet<Guid> set = type == RoleType.Read ? readRoles : writeRoles;

        _ = value ? set.Add(id) : set.Remove(id);

        UpdateSelectAllStates();
    }

    private void OnSelectAllChanged(RoleType type, bool? value)
    {
        value ??= true;

        HashSet<Guid> set = type == RoleType.Read ? readRoles : writeRoles;

        if (value.Value)
        {
            foreach (RoleDto r in roles)
            {
                _ = set.Add(r.Id);
            }
        }
        else
        {
            foreach (RoleDto r in roles)
            {
                _ = set.Remove(r.Id);
            }
        }

        UpdateSelectAllStates();
    }

    private void UpdateSelectAllStates()
    {
        selectAllRead = GetSelectAllState(RoleType.Read);
        selectAllWrite = GetSelectAllState(RoleType.Write);
    }

    private bool? GetSelectAllState(RoleType type)
    {
        bool any = roles.Any(r => IsAssigned(r.Id, type));
        bool all = roles.All(r => IsAssigned(r.Id, type));
        return all ? true : any ? null : false;
    }
}

