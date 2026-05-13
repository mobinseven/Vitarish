namespace Vitarish.Appointments.Interface.Plugins;

public record ShiftFormPluginContext(
    ShiftWriteDto Shift,
    EventCallback<Guid> OnSaved,
    EventCallback OnCancelled) : PluginContext();

public interface IShiftFormPlugin : IPlugin<ShiftFormPluginContext>;
