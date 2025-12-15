namespace Vitarish.Clients.Interface.Sea.Components.Roles;

public partial class RoleEditDialog : RolesBase
{
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter] public RoleDto? Role { get; set; }

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
            _ = await RolesService.AddAsync(name, CancellationToken.None);
        }
        else
        {
            await RolesService.UpdateAsync(Role.Id, name, CancellationToken.None);
        }

        MudDialog.Close(DialogResult.Ok(true));
    }

    private void Cancel() => MudDialog.Cancel();
}
