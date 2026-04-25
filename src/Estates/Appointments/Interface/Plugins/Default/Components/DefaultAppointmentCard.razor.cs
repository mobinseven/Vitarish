namespace Vitarish.Appointments.Interface.Plugins.Default.Components;

public partial class DefaultAppointmentCard : AppointmentsBase
{
    [Parameter, EditorRequired] public AppointmentReadDto Appointment { get; set; } = default!;
    [Parameter] public EventCallback OnEdit { get; set; }
    [Parameter] public EventCallback OnDeleted { get; set; }
}
