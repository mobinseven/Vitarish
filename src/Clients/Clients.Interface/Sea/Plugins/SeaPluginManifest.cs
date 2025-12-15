using System.Reflection;

namespace Vitarish.Clients.Interface.Sea.Plugins;

public sealed record SeaPluginManifest(
    IReadOnlyCollection<Assembly> McpToolAssemblies,
    IReadOnlyDictionary<string, IReadOnlyCollection<Type>> PluginToolTypes,
    IReadOnlyCollection<string> PluginIdentifiers);
