namespace Vitarish.Appointments.Interface.Plugins;

public record AppointmentCardPluginContext(
    AppointmentReadDto Appointment,
    EventCallback OnEdit,
    EventCallback OnDeleted) : PluginContext();

public interface IAppointmentCardPlugin : IPlugin<AppointmentCardPluginContext>;
