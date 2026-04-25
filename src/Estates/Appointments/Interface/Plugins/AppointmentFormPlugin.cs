namespace Vitarish.Appointments.Interface.Plugins;

public record AppointmentFormPluginContext(
    AppointmentWriteDto Appointment,
    EventCallback<Guid> OnSaved,
    EventCallback OnCancelled) : PluginContext();

public interface IAppointmentFormPlugin : IPlugin<AppointmentFormPluginContext>;
