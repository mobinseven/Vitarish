namespace Vitarish.Appointments.Interface.Components.ServiceTypes;

public partial class ServiceTypeDetail : ServiceTypesBase
{
    [Parameter, EditorRequired] public ServiceTypeReadDto ServiceType { get; set; } = default!;
    [Parameter] public EventCallback OnEdit { get; set; }
    [Parameter] public EventCallback OnDeleted { get; set; }

    private ServiceTypeReadDto? current;

    protected override void OnParametersSet() => current = ServiceType;

    private async Task DeleteAsync()
    {
        DialogParameters parameters = new()
        {
            { nameof(ConfirmDialog.ContentText), Localizer["DeleteItemConfirm", Localizer["ServiceType"]].Value },
            { nameof(ConfirmDialog.ButtonText), Localizer["Delete"].Value },
            { nameof(ConfirmDialog.Color), Color.Error }
        };

        IDialogReference dialog = await DialogService.ShowAsync<ConfirmDialog>(
            Localizer["DeleteItem", Localizer["ServiceType"]], parameters);
        DialogResult? result = await dialog.Result;

        if (result is null || result.Canceled)
        {
            return;
        }

        _ = await ServiceTypeManager.RemoveAsync(current!.Id, CancellationToken.None);
        await OnDeleted.InvokeAsync();
    }
}
