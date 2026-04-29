namespace Vitarish.Appointments.Interface.Components.Sessions;

public partial class AdminSessionDetail : SessionsBase
{
    [Parameter, EditorRequired] public SessionReadDto Session { get; set; } = default!;
    [Parameter] public EventCallback OnEdit { get; set; }
    [Parameter] public EventCallback OnDeleted { get; set; }
    [Parameter] public EventCallback OnStatusChanged { get; set; }

    private SessionReadDto current = default!;

    protected override void OnParametersSet()
    {
        current = Session;
    }

    private async Task CancelSessionAsync()
    {
        DialogParameters parameters = new()
        {
            { nameof(ConfirmDialog.ContentText), Localizer["ConfirmationQuestion"].Value },
            { nameof(ConfirmDialog.ButtonText), Localizer["CancelSession"].Value },
            { nameof(ConfirmDialog.Color), Color.Error }
        };

        IDialogReference dialog = await DialogService.ShowAsync<ConfirmDialog>(Localizer["CancelSession"], parameters);
        DialogResult? result = await dialog.Result;

        if (result is null || result.Canceled)
        {
            return;
        }

        await SessionsService.UpdateAsync(current.ToWrite(), CancellationToken.None);

        SessionReadDto? updated = await SessionsService.GetAsync(current.Id, CancellationToken.None).GetValueOrDefaultAsync();
        if (updated is not null)
        {
            current = updated;
        }

        await OnStatusChanged.InvokeAsync();
        await InvokeAsync(StateHasChanged);
    }

    private async Task DeleteSessionAsync()
    {
        DialogParameters parameters = new()
        {
            { nameof(ConfirmDialog.ContentText), Localizer["DeleteItemConfirm", Localizer["Session"]].Value },
            { nameof(ConfirmDialog.ButtonText), Localizer["Delete"].Value },
            { nameof(ConfirmDialog.Color), Color.Error }
        };

        IDialogReference dialog = await DialogService.ShowAsync<ConfirmDialog>(Localizer["DeleteItem", Localizer["Session"]], parameters);
        DialogResult? result = await dialog.Result;

        if (result is null || result.Canceled)
        {
            return;
        }

        await SessionsService.RemoveAsync(current.Id, CancellationToken.None);
        await OnDeleted.InvokeAsync();
    }
}
