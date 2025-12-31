namespace Vitarish.Sea.Interface.Components.Base;
/// <summary>
/// Describes a plugin that can provide a custom UI flow for creating events.
/// Implementations are resolved through dependency injection and rendered by
/// the events grid when a user starts a new event.
/// </summary>
public interface IPlugin<T>
    where T : PluginContext
{
    /// <summary>
    /// Gets the stable identifier that uniquely distinguishes this plugin
    /// from other event creation experiences.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the localized label displayed to users when selecting between
    /// available event creation forms.
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Gets the optional localized description that provides additional
    /// context for the experience surfaced by the plugin.
    /// </summary>
    string? Description { get; }

    /// <summary>
    /// Returns <see langword="true"/> when the plugin can be offered for the
    /// supplied <paramref name="context"/>. Plugins can use this hook to
    /// enforce role checks, feature flags, or prerequisite metadata.
    /// </summary>
    /// <param name="context">The runtime information describing the event creation request.</param>
    bool IsAvailable(T context);

    /// <summary>
    /// Builds the render fragment that hosts the creation experience. The
    /// fragment is placed into the standard dialog chrome and receives the
    /// same context data that was used to evaluate availability.
    /// </summary>
    /// <param name="context">The runtime information describing the event creation request.</param>
    RenderFragment Render(T context);
}
