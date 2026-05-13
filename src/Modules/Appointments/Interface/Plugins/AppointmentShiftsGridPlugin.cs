namespace Vitarish.Appointments.Interface.Plugins;

public record ShiftsGridPluginContext(
    EventCallback<Guid> OnClicked,
    EventCallback<DayOfWeek?> OnAdd) : PluginContext();

public interface IShiftsGridPlugin : IPlugin<ShiftsGridPluginContext>;
