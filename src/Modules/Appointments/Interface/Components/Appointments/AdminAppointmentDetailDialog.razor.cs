namespace Vitarish.Appointments.Interface.Components.Appointments;

public partial class AdminAppointmentDetailDialog : AppointmentsBase
{
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter, EditorRequired] public AppointmentReadDto Appointment { get; set; } = default!;

    private void OnDeletedInternal() => MudDialog.Close(DialogResult.Ok(true));

    private void Close() => MudDialog.Cancel();
}
