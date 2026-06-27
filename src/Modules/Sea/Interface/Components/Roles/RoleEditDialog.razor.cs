namespace Vitarish.Sea.Interface.Components.Roles;

public partial class RoleEditDialog : RolesBase
{
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter] public RoleReadDto? Role { get; set; }

    private string name = string.Empty;

    protected override void OnInitialized()
    {
        if (Role is not null)
        {
            name = Role.Name;
        }
    }

    private async Task SaveAsync()
    {
        if (Role is null)
        {
            _ = await CommandOrNotifyAsync(SeaClient.CreateRoleAsync(new RoleWriteDto(name), CancellationToken.None));
        }
        else
        {
            await CommandOrNotifyAsync(SeaClient.UpdateRoleAsync(new RoleWriteDto(name) { Id = Role.Id }, CancellationToken.None));
        }

        MudDialog.Close(DialogResult.Ok(true));
    }

    private void Cancel() => MudDialog.Cancel();
}
