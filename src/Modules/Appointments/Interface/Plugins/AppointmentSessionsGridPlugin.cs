namespace Vitarish.Appointments.Interface.Plugins;

public record SessionsGridPluginContext(
    EventCallback<Guid> OnClicked,
    EventCallback OnAdd) : PluginContext();

public interface ISessionsGridPlugin : IPlugin<SessionsGridPluginContext>;
