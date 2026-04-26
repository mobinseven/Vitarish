namespace Vitarish.Appointments.Interface.Plugins;

public record AppointmentsGridPluginContext(
    EventCallback<Guid> OnClicked,
    EventCallback OnAdd) : PluginContext();

public interface IAppointmentsGridPlugin : IPlugin<AppointmentsGridPluginContext>;
