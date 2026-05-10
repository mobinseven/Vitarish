namespace Vitarish.Appointments.Interface.Components.Reservations;

public partial class ReservationDetailDialog : ReservationsBase
{
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter, EditorRequired] public ReservationReadDto Reservation { get; set; } = default!;

    private void OnDeletedInternal() => MudDialog.Close(DialogResult.Ok(true));

    private void OnStatusChanged() { }

    private void Close() => MudDialog.Cancel();
}
