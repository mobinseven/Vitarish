namespace Vitarish.Sea.Interface.Components.Users;

public partial class UserRolesDisplay : UsersBase
{
    [Parameter, EditorRequired] public UserDto User { get; set; } = default!;

    private List<(string Name, RoleType Type)> roles = [];

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        IReadOnlyList<UserRoleReadDto> userRoles = [.. await UserRolesService.GetListAsync(new UserRoleDescriptorDto { UserId = Guid.Parse(User.Id) }, CancellationToken.None).GetValueOrDefaultAsync() ?? []];

        if (userRoles.Count == 0)
        {
            return;
        }

        IReadOnlyList<RoleReadDto> roleDtos = [.. await RolesService.GetListAsync(new RoleDescriptorDto(), CancellationToken.None).GetValueOrDefaultAsync() ?? []];
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

