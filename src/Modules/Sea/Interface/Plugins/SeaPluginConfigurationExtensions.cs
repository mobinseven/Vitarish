using System.Reflection;
using Vitarish.Interfaces.Base.Localization;
using Vitarish.Sea.Interface.Plugins.Clinic.Projects;
using Vitarish.Sea.Interface.Plugins.Gym.Projects;

namespace Vitarish.Sea.Interface.Plugins;

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
                    _ = services.AddScoped<IProjectCreationFormPlugin, ClinicProjectCreationFormPlugin>();
                    _ = services.AddScoped<IProjectGridCellDisplayPlugin, ClinicProjectGridCellDisplayPlugin>();
                },
                [
                    typeof(ClinicProjectCreationForm),
                typeof(ClinicProjectGridCellDisplay)
                ]);

            PluginDescriptor gymDescriptor = new(
                services =>
                {
                    _ = services.AddScoped<IProjectCreationFormPlugin, GymTraineeProjectCreationFormPlugin>();
                    _ = services.AddScoped<IProjectGridCellDisplayPlugin, GymProjectGridCellDisplayPlugin>();
                },
                [
                    typeof(GymTraineeProjectCreationForm),
                typeof(GymProjectGridCellDisplay)
                ]);

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

                _ = loadedPluginIdentifiers.Add(trimmedIdentifier);
            }

            resourceTypes.Add(typeof(Strings));
            _ = services.ConfigureCompositeStringLocalizer([.. resourceTypes]);

            return new SeaPluginManifest([.. toolAssemblies], pluginToolTypes, [.. loadedPluginIdentifiers]);
        }
    }

    private sealed record PluginDescriptor(
        Action<IServiceCollection> ConfigureServices,
        IReadOnlyCollection<Type> ResourceTypes);
}
