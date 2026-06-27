namespace Vitarish.Sea.Interface.Components.Roles;

public partial class RolesGrid : RolesBase
{
    private List<RoleReadDto> roles = [];

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        roles = [.. await ValueOrNotifyAsync(SeaClient.ListRolesAsync(CancellationToken.None)) ?? []];
    }

    private async Task AddRole()
    {
        IDialogReference dialog = await DialogService.ShowAsync<RoleEditDialog>((string?)null);
        DialogResult? result = await dialog.Result;

        if (result is null)
        {
            return;
        }

        if (result.Canceled)
        {
            return;
        }
        else
        {
            roles = [.. await ValueOrNotifyAsync(SeaClient.ListRolesAsync(CancellationToken.None)) ?? []];
            StateHasChanged();
        }
    }

    private async Task EditRole(RoleReadDto role)
    {
        DialogParameters parameters = new()
        {
            { nameof(RoleEditDialog.Role), role }
        };
        IDialogReference dialog = await DialogService.ShowAsync<RoleEditDialog>(null, parameters);
        DialogResult? result = await dialog.Result;

        if (result is null)
        {
            return;
        }

        if (result.Canceled)
        {
            return;
        }
        else
        {
            roles = [.. await ValueOrNotifyAsync(SeaClient.ListRolesAsync(CancellationToken.None)) ?? []];
            StateHasChanged();
        }
    }

    private async Task DeleteRole(RoleReadDto role)
    {
        DialogParameters parameters = new()
        {
            { nameof(ConfirmDialog.ContentText), Localizer["ConfirmationQuestion"].Value },
            { nameof(ConfirmDialog.ButtonText), Localizer["DeleteItem", Localizer["Role"]].Value },
            { nameof(ConfirmDialog.Color), Color.Error }
        };
        IDialogReference dialog = await DialogService.ShowAsync<ConfirmDialog>(Localizer["DeleteItem", Localizer["Role"]], parameters);
        DialogResult? result = await dialog.Result;

        if (result is null)
        {
            return;
        }

        if (result.Canceled)
        {
            return;
        }
        else
        {
            await CommandOrNotifyAsync(SeaClient.DeleteRoleAsync(role.Id, CancellationToken.None));
            _ = roles.Remove(role);
            StateHasChanged();
        }
    }
}
