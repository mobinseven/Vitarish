using System.Reflection;

namespace Vitarish.Sea.Interface.Plugins;

public sealed record SeaPluginManifest(
    IReadOnlyCollection<Assembly> McpToolAssemblies,
    IReadOnlyDictionary<string, IReadOnlyCollection<Type>> PluginToolTypes,
    IReadOnlyCollection<string> PluginIdentifiers);
