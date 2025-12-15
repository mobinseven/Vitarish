namespace Vitarish.Clients.Interface.Sea.Components.Users;

public partial class UserRolesDisplay : UsersBase
{
    [Parameter, EditorRequired] public UserDto User { get; set; } = default!;

    private List<(string Name, RoleType Type)> roles = [];

    protected override async Task OnInitializedAsync()
    {
        IReadOnlyList<UserRoleDto> userRoles = await UserRolesService.GetListAsync(
                new UserRoleDescriptorDto { UserId = Guid.Parse(User.Id) },
                CancellationToken.None)
            .EnsureValueAsync("Load user roles");

        if (userRoles.Count == 0)
        {
            return;
        }

        IReadOnlyList<RoleDto> roleDtos = await RolesService.GetListAsync(new RoleDescriptorDto(), CancellationToken.None)
            .EnsureValueAsync("Load roles");
        Dictionary<Guid, string> roleNames = roleDtos.ToDictionary(r => r.Id, r => r.Name);

        roles = [.. userRoles
            .Where(r => roleNames.ContainsKey(r.RoleId))
            .Select(r => (roleNames[r.RoleId], r.Type))];
    }

    private static Color GetColor(RoleType type)
        => type switch
        {
            RoleType.Read => Color.Info,
            RoleType.Write => Color.Error,
            _ => Color.Default
        };
}

