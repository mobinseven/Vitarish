namespace Vitarish.Appointments.Interface.Plugins;

public record SessionFormPluginContext(
    AppointmentSessionWriteDto Session,
    EventCallback<Guid> OnSaved,
    EventCallback OnCancelled) : PluginContext();

public interface ISessionFormPlugin : IPlugin<SessionFormPluginContext>;
