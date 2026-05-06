namespace Vitarish.Appointments.Interface.Components.Reservations;

public partial class ReservationDetail : ReservationsBase
{
    [Parameter, EditorRequired] public ReservationReadDto Reservation { get; set; } = default!;
    [Parameter] public EventCallback OnCancelled { get; set; }

    private ReservationReadDto current = default!;

    protected override void OnParametersSet()
    {
        current = Reservation;
    }

    private async Task CancelReservationAsync()
    {
        DialogParameters parameters = new()
        {
            { nameof(ConfirmDialog.ContentText), Localizer["ConfirmationQuestion"].Value },
            { nameof(ConfirmDialog.ButtonText), Localizer["CancelItem", Localizer["Reservation"]].Value },
            { nameof(ConfirmDialog.Color), Color.Error }
        };

        IDialogReference dialog = await DialogService.ShowAsync<ConfirmDialog>(Localizer["CancelItem", Localizer["Reservation"]], parameters);
        DialogResult? result = await dialog.Result;

        if (result is null || result.Canceled)
        {
            return;
        }

        await ReservationManager.CancelReservationAsync(current.Id, CancellationToken.None);
        await OnCancelled.InvokeAsync();
    }
}
