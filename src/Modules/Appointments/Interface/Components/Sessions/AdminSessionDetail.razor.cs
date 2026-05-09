namespace Vitarish.Appointments.Interface.Components.Sessions;

public partial class AdminSessionDetail : SessionsBase
{
    [Parameter, EditorRequired] public SessionReadDto Session { get; set; } = default!;
    [Parameter] public EventCallback OnEdit { get; set; }
    [Parameter] public EventCallback OnDeleted { get; set; }
    [Parameter] public EventCallback OnStatusChanged { get; set; }

    private async Task ChangeStatusAsync(SessionStatus status)
    {
        string label = status switch
        {
            SessionStatus.Active => Localizer["ActivateItem", Localizer["Session"]].Value,
            SessionStatus.Paused => Localizer["PauseItem", Localizer["Session"]].Value,
            SessionStatus.Closed => Localizer["CloseItem", Localizer["Session"]].Value,
            SessionStatus.Cancelled => Localizer["CancelItem", Localizer["Session"]].Value,
            SessionStatus.Archived => Localizer["ArchiveItem", Localizer["Session"]].Value,
            _ => Localizer["Confirm"].Value
        };

        Color color = status is SessionStatus.Cancelled or SessionStatus.Archived ? Color.Error : Color.Primary;

        DialogParameters parameters = new()
        {
            { nameof(ConfirmDialog.ContentText), Localizer["ConfirmationQuestion"].Value },
            { nameof(ConfirmDialog.ButtonText), label },
            { nameof(ConfirmDialog.Color), color }
        };

        IDialogReference dialog = await DialogService.ShowAsync<ConfirmDialog>(label, parameters);
        DialogResult? result = await dialog.Result;

        if (result is null || result.Canceled)
        {
            return;
        }

        EntityResult entityResult = status switch
        {
            SessionStatus.Active => await SessionManager.ActivateSessionAsync(Session.Id, CancellationToken.None),
            SessionStatus.Paused => await SessionManager.PauseSessionAsync(Session.Id, CancellationToken.None),
            SessionStatus.Closed => await SessionManager.CloseSessionAsync(Session.Id, CancellationToken.None),
            SessionStatus.Cancelled => await SessionManager.CancelSessionAsync(Session.Id, CancellationToken.None),
            SessionStatus.Archived => await SessionManager.ArchiveSessionAsync(Session.Id, CancellationToken.None),
            _ => EntityResult.Success()
        };

        if (entityResult.IsSuccess)
        {
            SessionReadDto? updated = await SessionManager.GetAsync(Session.Id, CancellationToken.None).GetValueOrDefaultAsync();
            if (updated is not null)
            {
                Session = updated;
            }

            await OnStatusChanged.InvokeAsync();
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task AddReservationAsync(DateOnly date)
    {
        DialogParameters parameters = new()
        {
            { nameof(AdminReservationForSessionDialog.SessionId), Session.Id },
            { nameof(AdminReservationForSessionDialog.Date), date }
        };

        IDialogReference dialog = await DialogService.ShowAsync<AdminReservationForSessionDialog>(
            Localizer["AddItem", Localizer["Reservation"]], parameters);
        DialogResult? result = await dialog.Result;

        if (result is not null && !result.Canceled)
        {
            await OnStatusChanged.InvokeAsync();
        }
    }
}
