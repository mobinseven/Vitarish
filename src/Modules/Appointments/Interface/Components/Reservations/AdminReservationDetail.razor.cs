namespace Vitarish.Appointments.Interface.Components.Reservations;

public partial class AdminReservationDetail : ReservationsBase
{
    [Parameter, EditorRequired] public ReservationReadDto Reservation { get; set; } = default!;
    [Parameter] public EventCallback OnDeleted { get; set; }
    [Parameter] public EventCallback OnStatusChanged { get; set; }

    private ReservationReadDto current = default!;

    protected override void OnParametersSet()
    {
        current = Reservation;
    }

    private async Task ChangeStatusAsync(ReservationStatus status)
    {
        string label = status switch
        {
            ReservationStatus.Confirmed => Localizer["ConfirmReservation"].Value,
            ReservationStatus.Completed => Localizer["CompleteReservation"].Value,
            ReservationStatus.Cancelled => Localizer["CancelReservation"].Value,
            ReservationStatus.Rejected => Localizer["RejectReservation"].Value,
            ReservationStatus.Present => Localizer["PresentReservation"].Value,
            _ => Localizer["Confirm"].Value
        };

        Color color = status is ReservationStatus.Cancelled or ReservationStatus.Rejected ? Color.Error : Color.Primary;

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
            ReservationStatus.Confirmed => await ReservationManager.ConfirmReservationAsync(current.Id, CancellationToken.None),
            ReservationStatus.Cancelled => await ReservationManager.CancelReservationAsync(current.Id, CancellationToken.None),
            ReservationStatus.Rejected => await ReservationManager.RejectReservationAsync(current.Id, CancellationToken.None),
            _ => await ReservationManager.UpdateAsync(current.ToWrite() with { Status = status }, CancellationToken.None).ContinueWith(_ => EntityResult.Success())
        };

        if (entityResult.IsSuccess)
        {
            ReservationReadDto? updated = await ReservationManager.GetAsync(current.Id, CancellationToken.None).GetValueOrDefaultAsync();
            if (updated is not null)
            {
                current = updated;
            }

            await OnStatusChanged.InvokeAsync();
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task DeleteReservationAsync()
    {
        DialogParameters parameters = new()
        {
            { nameof(ConfirmDialog.ContentText), Localizer["DeleteItemConfirm", Localizer["Reservation"]].Value },
            { nameof(ConfirmDialog.ButtonText), Localizer["Delete"].Value },
            { nameof(ConfirmDialog.Color), Color.Error }
        };

        IDialogReference dialog = await DialogService.ShowAsync<ConfirmDialog>(Localizer["DeleteItem", Localizer["Reservation"]], parameters);
        DialogResult? result = await dialog.Result;

        if (result is null || result.Canceled)
        {
            return;
        }

        await ReservationManager.RemoveAsync(current.Id, CancellationToken.None);
        await OnDeleted.InvokeAsync();
    }
}
