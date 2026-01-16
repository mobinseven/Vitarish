namespace Vitarish.Sea.Interface.Components.Base;
/// <summary>
/// Describes a plugin that can provide a custom UI flow for creating events.
/// Implementations are resolved through dependency injection and rendered by
/// the events grid when a user starts a new event.
/// </summary>
public interface IPlugin<T>
    where T : PluginContext
{
    string Id { get; }
    string DisplayName { get; }
    string? Description { get; }
    bool IsAvailable(T context);
    RenderFragment Render(T context);
}
