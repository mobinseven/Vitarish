namespace Vitarish.Sea.Interface.Components.Events;

public partial class EventRolesEditDialog : SeaCommonBase
{
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;
        
    [Parameter] public EventReadDto? Event { get; set; }

    private HashSet<Guid> readRoleIds = [];
    private HashSet<Guid> writeRoleIds = [];
    private HashSet<Guid> initialReadRoleIds = [];
    private HashSet<Guid> initialWriteRoleIds = [];
    private List<RoleReadDto> roles = [];

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        if (Event is null)
        {
            MudDialog.Cancel();
            return;
        }
        roles = [.. await ValueOrNotifyAsync(SeaClient.ListRolesAsync(CancellationToken.None)) ?? []];
        (IReadOnlyList<Guid> ReadRoleIds, IReadOnlyList<Guid> WriteRoleIds) = (await ValueOrNotifyAsync(SeaClient.GetEventReadRoleIdsAsync(Event.Id, CancellationToken.None)) ?? [], await ValueOrNotifyAsync(SeaClient.GetEventWriteRoleIdsAsync(Event.Id, CancellationToken.None)) ?? []);
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
            await CommandOrNotifyAsync(SeaClient.AssignEventRoleAsync(new EventRoleWriteDto(Event.Id, r, RoleType.Read), CancellationToken.None));
        }

        foreach (Guid r in initialReadRoleIds.Except(readRoleIds))
        {
            await CommandOrNotifyAsync(SeaClient.UnassignEventRoleAsync(r, CancellationToken.None));
        }

        foreach (Guid r in writeRoleIds.Except(initialWriteRoleIds))
        {
            await CommandOrNotifyAsync(SeaClient.AssignEventRoleAsync(new EventRoleWriteDto(Event.Id, r, RoleType.Write), CancellationToken.None));
        }

        foreach (Guid r in initialWriteRoleIds.Except(writeRoleIds))
        {
            await CommandOrNotifyAsync(SeaClient.UnassignEventRoleAsync(r, CancellationToken.None));
        }

        MudDialog.Close(DialogResult.Ok(true));
    }

    private void OnReadRoleChanged(Guid id, bool value) => _ = value ? readRoleIds.Add(id) : readRoleIds.Remove(id);

    private void OnWriteRoleChanged(Guid id, bool value) => _ = value ? writeRoleIds.Add(id) : writeRoleIds.Remove(id);

    private void Cancel() => MudDialog.Cancel();
}
