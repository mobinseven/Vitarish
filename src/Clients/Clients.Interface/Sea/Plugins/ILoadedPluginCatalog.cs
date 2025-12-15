namespace Vitarish.Clients.Interface.Sea.Plugins;
/// <summary>
/// Provides read-only access to the set of Sea plugins that are loaded in the
/// current host. Components can consult this catalog to toggle optional UI
/// elements without duplicating plugin identifiers.
/// </summary>
public interface ILoadedPluginCatalog
{
    /// <summary>
    /// Gets the identifiers for the plugins that are currently loaded.
    /// </summary>
    IReadOnlyCollection<string> Identifiers { get; }

    /// <summary>
    /// Returns <c>true</c> when the specified plugin identifier is present in
    /// the current catalog.
    /// </summary>
    /// <param name="identifier">The plugin identifier to check.</param>
    bool IsLoaded(string identifier);
}
