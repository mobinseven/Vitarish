namespace Vitarish.Appointments.Interface.Components.Services;

public partial class ServiceDetail : ServicesBase
{
    [Parameter, EditorRequired] public ServiceReadDto Service { get; set; } = default!;
    [Parameter] public EventCallback OnEdit { get; set; }
    [Parameter] public EventCallback OnDeleted { get; set; }

    private ServiceReadDto? current;

    protected override void OnParametersSet() => current = Service;

    private async Task DeleteAsync()
    {
        DialogParameters parameters = new()
        {
            { nameof(ConfirmDialog.ContentText), Localizer["DeleteItemConfirm", Localizer["Service"]].Value },
            { nameof(ConfirmDialog.ButtonText), Localizer["Delete"].Value },
            { nameof(ConfirmDialog.Color), Color.Error }
        };

        IDialogReference dialog = await DialogService.ShowAsync<ConfirmDialog>(
            Localizer["DeleteItem", Localizer["Service"]], parameters);
        DialogResult? result = await dialog.Result;

        if (result is null || result.Canceled)
        {
            return;
        }

        _ = await ServiceManager.RemoveAsync(current!.Id, CancellationToken.None);
        await OnDeleted.InvokeAsync();
    }
}
