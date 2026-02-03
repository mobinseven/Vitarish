namespace Vitarish.Sea.Interface.Components.Events;

public partial class EventRolesDisplay : EventsBase
{
    [Parameter, EditorRequired] public EventDto Event { get; set; } = default!;

    private readonly List<RoleChip> roles = [];

    private record RoleChip(string Name, RoleType Type);

    protected override Task OnParametersSetAsync() => LoadRolesAsync();

    private async Task LoadRolesAsync()
    {
        roles.Clear();
        (IReadOnlyList<Guid> ReadRoleIds, IReadOnlyList<Guid> WriteRoleIds) = await EventRolesService.GetIdsAsync(Event.Id, CancellationToken.None);
        IReadOnlyList<RoleDto> allRoles = await RolesService.GetListAsync(new RoleDescriptorDto(), CancellationToken.None).GetValueOrDefaultAsync() ?? [];
        foreach (Guid id in ReadRoleIds)
        {
            RoleDto? role = allRoles.FirstOrDefault(r => r.Id == id);
            if (role is not null)
            {
                roles.Add(new RoleChip(role.Name, RoleType.Read));
            }
        }
        foreach (Guid id in WriteRoleIds)
        {
            RoleDto? role = allRoles.FirstOrDefault(r => r.Id == id);
            if (role is not null)
            {
                roles.Add(new RoleChip(role.Name, RoleType.Write));
            }
        }
    }

    private static Color GetColor(RoleType type)
        => type == RoleType.Write ? Color.Primary : Color.Default;
}
