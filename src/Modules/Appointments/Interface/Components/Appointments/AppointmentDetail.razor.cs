namespace Vitarish.Appointments.Interface.Components.Appointments;

public partial class AppointmentDetail : AppointmentsBase
{
    [Parameter, EditorRequired] public AppointmentReadDto Appointment { get; set; } = default!;
    [Parameter] public EventCallback OnReschedule { get; set; }
    [Parameter] public EventCallback OnCancelled { get; set; }

    private AppointmentReadDto current = default!;

    protected override void OnParametersSet()
    {
        current ??= Appointment;
    }

    private async Task CancelAppointmentAsync()
    {
        DialogParameters parameters = new()
        {
            { nameof(ConfirmDialog.ContentText), Localizer["ConfirmationQuestion"].Value },
            { nameof(ConfirmDialog.ButtonText), Localizer["CancelAppointment"].Value },
            { nameof(ConfirmDialog.Color), Color.Error }
        };

        IDialogReference dialog = await DialogService.ShowAsync<ConfirmDialog>(Localizer["CancelAppointment"], parameters);
        DialogResult? result = await dialog.Result;

        if (result is null || result.Canceled)
        {
            return;
        }

        await AppointmentsService.UpdateStatusAsync(current.Id, AppointmentStatus.Cancelled, CancellationToken.None);
        await OnCancelled.InvokeAsync();
    }
}
