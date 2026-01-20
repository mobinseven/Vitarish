namespace Vitarish.Sea.Interface.Components.Roles;

public partial class RolesGrid : RolesBase
{
    private List<RoleDto> roles = [];

    protected override async Task OnInitializedAsync()
     {
        await base.OnInitializedAsync();
        roles = [.. await RolesService.GetListAsync(new RoleDescriptorDto(), CancellationToken.None).GetValueOrDefaultAsync() ?? []];
    }

    private async Task AddRole()
    {
        IDialogReference dialog = await DialogService.ShowAsync<RoleEditDialog>(Localizer["Role"]);
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
            roles = [.. await RolesService.GetListAsync(new RoleDescriptorDto(), CancellationToken.None)
                .GetValueOrDefaultAsync() ?? []];
            StateHasChanged();
        }
    }

    private async Task EditRole(RoleDto role)
    {
        DialogParameters parameters = new()
        {
            { nameof(RoleEditDialog.Role), role }
        };
        IDialogReference dialog = await DialogService.ShowAsync<RoleEditDialog>(Localizer["Role"], parameters);
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
            roles = (await RolesService.GetListAsync(new RoleDescriptorDto(), CancellationToken.None)
                .EnsureValueAsync("Reload roles")).ToList();
            StateHasChanged();
        }
    }

    private async Task DeleteRole(RoleDto role)
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
            await RolesService.RemoveAsync(role.Id, CancellationToken.None).EnsureSuccessAsync("Remove role");
            _ = roles.Remove(role);
            StateHasChanged();
        }
    }
}
