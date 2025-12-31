namespace Vitarish.Sea.Interface.Plugins;
/// <summary>
/// Default implementation of <see cref="ILoadedPluginCatalog"/> that exposes
/// the plugin identifiers recorded in the <see cref="SeaPluginManifest"/>.
/// </summary>
public sealed class SeaPluginCatalog : ILoadedPluginCatalog
{
    private readonly HashSet<string> lookup;

    public SeaPluginCatalog(SeaPluginManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        Identifiers = manifest.PluginIdentifiers?.ToArray() ?? Array.Empty<string>();
        lookup = new HashSet<string>(Identifiers, StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public IReadOnlyCollection<string> Identifiers { get; }

    /// <inheritdoc />
    public bool IsLoaded(string identifier)
        => identifier is { Length: > 0 } && lookup.Contains(identifier);
}
