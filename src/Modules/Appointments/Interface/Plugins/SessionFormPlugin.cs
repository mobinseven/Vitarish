namespace Vitarish.Appointments.Interface.Plugins;

public record SessionFormPluginContext(
    SessionWriteDto Session,
    EventCallback<Guid> OnSaved,
    EventCallback OnCancelled) : PluginContext();

public interface ISessionFormPlugin : IPlugin<SessionFormPluginContext>;
