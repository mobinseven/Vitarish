namespace Vitarish.Sea.Interface.Components.Events;

public partial class EventRolesEditDialog : SeaCommonBase
{
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;
    [Inject] private IRolesService RolesService { get; set; } = default!;
    [Inject] private IEventRolesService EventRolesService { get; set; } = default!;

    [Parameter] public EventDto? Event { get; set; }

    private HashSet<Guid> readRoleIds = [];
    private HashSet<Guid> writeRoleIds = [];
    private HashSet<Guid> initialReadRoleIds = [];
    private HashSet<Guid> initialWriteRoleIds = [];
    private List<RoleDto> roles = [];

    protected override async Task OnInitializedAsync()
    {
        if (Event is null)
        {
            MudDialog.Cancel();
            return;
        }
        roles = (await RolesService.GetListAsync(new RoleDescriptorDto(), CancellationToken.None)
            .EnsureValueAsync("Load roles")).ToList();
        (IReadOnlyList<Guid> ReadRoleIds, IReadOnlyList<Guid> WriteRoleIds) = await EventRolesService.GetIdsAsync(Event.Id, CancellationToken.None);
        readRoleIds = [.. ReadRoleIds];
        writeRoleIds = [.. WriteRoleIds];
        initialReadRoleIds = [.. readRoleIds];
        initialWriteRoleIds = [.. writeRoleIds];
    }

    private async Task SaveAsync()
    {
        if (Event is null)
        {
            MudDialog.Cancel();
            return;
        }
        foreach (Guid r in readRoleIds.Except(initialReadRoleIds))
        {
            await EventRolesService.AssignAsync(Event.Id, r, false, CancellationToken.None);
        }

        foreach (Guid r in initialReadRoleIds.Except(readRoleIds))
        {
            await EventRolesService.UnassignAsync(Event.Id, r, false, CancellationToken.None);
        }

        foreach (Guid r in writeRoleIds.Except(initialWriteRoleIds))
        {
            await EventRolesService.AssignAsync(Event.Id, r, true, CancellationToken.None);
        }

        foreach (Guid r in initialWriteRoleIds.Except(writeRoleIds))
        {
            await EventRolesService.UnassignAsync(Event.Id, r, true, CancellationToken.None);
        }

        MudDialog.Close(DialogResult.Ok(true));
    }

    private void OnReadRoleChanged(Guid id, bool value) => _ = value ? readRoleIds.Add(id) : readRoleIds.Remove(id);

    private void OnWriteRoleChanged(Guid id, bool value) => _ = value ? writeRoleIds.Add(id) : writeRoleIds.Remove(id);

    private void Cancel() => MudDialog.Cancel();
}
