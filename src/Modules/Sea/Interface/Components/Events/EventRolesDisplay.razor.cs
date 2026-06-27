namespace Vitarish.Sea.Interface.Components.Events;

public partial class EventRolesDisplay : EventsBase
{
    [Parameter, EditorRequired] public EventReadDto Event { get; set; } = default!;

    private readonly List<RoleChip> roles = [];

    private record RoleChip(string Name, RoleType Type);

    protected override Task OnParametersSetAsync() => LoadRolesAsync();

    private async Task LoadRolesAsync()
    {
        roles.Clear();
        (IReadOnlyList<Guid> ReadRoleIds, IReadOnlyList<Guid> WriteRoleIds) = (await ValueOrNotifyAsync(SeaClient.GetEventReadRoleIdsAsync(Event.Id, CancellationToken.None)) ?? [], await ValueOrNotifyAsync(SeaClient.GetEventWriteRoleIdsAsync(Event.Id, CancellationToken.None)) ?? []);
        IReadOnlyList<RoleReadDto> allRoles = await ValueOrNotifyAsync(SeaClient.ListRolesAsync(CancellationToken.None)) ?? [];
        foreach (Guid id in ReadRoleIds)
        {
            RoleReadDto? role = allRoles.FirstOrDefault(r => r.Id == id);
            if (role is not null)
            {
                roles.Add(new RoleChip(role.Name, RoleType.Read));
            }
        }
        foreach (Guid id in WriteRoleIds)
        {
            RoleReadDto? role = allRoles.FirstOrDefault(r => r.Id == id);
            if (role is not null)
            {
                roles.Add(new RoleChip(role.Name, RoleType.Write));
            }
        }
    }

    private static Color GetColor(RoleType type)
        => type == RoleType.Write ? Color.Primary : Color.Default;
}
