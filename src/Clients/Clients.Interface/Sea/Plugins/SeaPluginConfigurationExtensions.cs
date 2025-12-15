using System.Reflection;
using Vitarish.Clients.Interface.Localization;
using Vitarish.Clients.Interface.Sea.Plugins.Clinic.Projects;
using Vitarish.Clients.Interface.Sea.Plugins.Clinic.Tools;
using Vitarish.Clients.Interface.Sea.Plugins.Gym.Projects;

namespace Vitarish.Clients.Interface.Sea.Plugins;

public static class SeaPluginConfigurationExtensions
{
    extension(IServiceCollection services)
    {
        public SeaPluginManifest ConfigureSeaPlugins(
        IEnumerable<string?> pluginIdentifiers)
        {
            ArgumentNullException.ThrowIfNull(pluginIdentifiers);

            PluginDescriptor clinicDescriptor = new(
                services =>
                {
                    _ = services.AddScoped<IProjectCreationFormPlugin, ClinicPatientIntakeProjectCreationFormPlugin>();
                    _ = services.AddScoped<IProjectGridCellDisplayPlugin, ClinicProjectGridCellDisplayPlugin>();
                },
                [
                    typeof(ClinicPatientIntakeProjectCreationForm),
                typeof(ClinicProjectGridCellDisplay)
                ],
                [typeof(ClinicProjectsTools)]);

            PluginDescriptor gymDescriptor = new(
                services =>
                {
                    _ = services.AddScoped<IProjectCreationFormPlugin, GymTraineeProjectCreationFormPlugin>();
                    _ = services.AddScoped<IProjectGridCellDisplayPlugin, GymProjectGridCellDisplayPlugin>();
                },
                [
                    typeof(GymTraineeProjectCreationForm),
                typeof(GymProjectGridCellDisplay)
                ],
                Array.Empty<Type>());

            Dictionary<string, PluginDescriptor> descriptors = new(StringComparer.OrdinalIgnoreCase)
            {
                ["clinic"] = clinicDescriptor,
                ["gym"] = gymDescriptor,
            };

            HashSet<PluginDescriptor> configuredPlugins = [];
            List<Type> resourceTypes = [];
            HashSet<Assembly> toolAssemblies = [];
            Dictionary<string, IReadOnlyCollection<Type>> pluginToolTypes = new(StringComparer.OrdinalIgnoreCase);
            HashSet<string> loadedPluginIdentifiers = new(StringComparer.OrdinalIgnoreCase);

            foreach (string? identifier in pluginIdentifiers)
            {
                if (identifier is not { Length: > 0 } value)
                {
                    continue;
                }

                string trimmedIdentifier = value.Trim();

                if (trimmedIdentifier.Length == 0)
                {
                    continue;
                }

                if (!descriptors.TryGetValue(trimmedIdentifier!, out PluginDescriptor? descriptor))
                {
                    continue;
                }

                PluginDescriptor resolvedDescriptor = descriptor!;

                if (!configuredPlugins.Add(resolvedDescriptor))
                {
                    continue;
                }

                resolvedDescriptor.ConfigureServices(services);
                resourceTypes.AddRange(resolvedDescriptor.ResourceTypes);
                foreach (Type toolType in resolvedDescriptor.McpToolTypes)
                {
                    _ = toolAssemblies.Add(toolType.Assembly);
                }

                if (resolvedDescriptor.McpToolTypes.Count > 0)
                {
                    pluginToolTypes[trimmedIdentifier] = resolvedDescriptor.McpToolTypes;
                }

                _ = loadedPluginIdentifiers.Add(trimmedIdentifier);
            }

            resourceTypes.Add(typeof(Strings));
            _ = services.ConfigureCompositeStringLocalizer([.. resourceTypes]);

            return new SeaPluginManifest([.. toolAssemblies], pluginToolTypes, [.. loadedPluginIdentifiers]);
        }
    }

    private sealed record PluginDescriptor(
        Action<IServiceCollection> ConfigureServices,
        IReadOnlyCollection<Type> ResourceTypes,
        IReadOnlyCollection<Type> McpToolTypes);
}
