using Vitarish.Users.Interface.Components.Users;

namespace Vitarish.Appointments.Interface.Components.ServiceProviders;

public partial class ServiceProviderDetail : ServiceProvidersBase
{
    [Parameter, EditorRequired] public ServiceProviderReadDto Provider { get; set; } = default!;
    [Parameter] public EventCallback OnEdit { get; set; }
    [Parameter] public EventCallback OnDeleted { get; set; }

    private ServiceProviderReadDto? current;
    private string? linkUserId;
    private readonly UserFindOrCreateOptions linkOptions = new()
    {
        // Unlinking is handled by the explicit unlink buttons per user; only linking is done here.
        AllowUnlink = false,
        AllowCreate = true,
        EnsureTenantUserRoleOnLink = false
    };

    protected override void OnParametersSet()
    {
        current = Provider;
    }

    private async Task LinkUserAsync(string? userId)
    {
        if (string.IsNullOrWhiteSpace(userId) || current is null)
        {
            return;
        }

        await ServiceProviderManager.LinkUserAsync(current.Id, userId, CancellationToken.None);
        linkUserId = null;
        await RefreshAsync();
    }

    private async Task UnlinkUserAsync(string userId)
    {
        if (current is null)
        {
            return;
        }

        await ServiceProviderManager.UnlinkUserAsync(current.Id, userId, CancellationToken.None);
        await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        ServiceProviderReadDto? updated = await ServiceProviderManager
            .GetAsync(current!.Id, CancellationToken.None)
            .GetValueOrDefaultAsync();
        if (updated is not null)
        {
            current = updated;
        }

        await InvokeAsync(StateHasChanged);
    }

    private async Task DeleteAsync()
    {
        DialogParameters parameters = new()
        {
            { nameof(ConfirmDialog.ContentText), Localizer["DeleteItemConfirm", Localizer["ServiceProvider"]].Value },
            { nameof(ConfirmDialog.ButtonText), Localizer["Delete"].Value },
            { nameof(ConfirmDialog.Color), Color.Error }
        };

        IDialogReference dialog = await DialogService.ShowAsync<ConfirmDialog>(
            Localizer["DeleteItem", Localizer["ServiceProvider"]], parameters);
        DialogResult? result = await dialog.Result;

        if (result is null || result.Canceled)
        {
            return;
        }

        await ServiceProviderManager.RemoveAsync(current!.Id, CancellationToken.None);
        await OnDeleted.InvokeAsync();
    }
}
