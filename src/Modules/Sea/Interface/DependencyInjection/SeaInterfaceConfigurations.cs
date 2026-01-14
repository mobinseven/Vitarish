using Vitarish.Sea.Interface.Components.Attachments;
using Vitarish.Sea.Interface.Components.Reminders;

namespace Vitarish.Sea.Interface.DependencyInjection;

public static class SeaInterfaceConfigurations
{
    extension(IServiceCollection services)
    {
        public IServiceCollection ConfigureSeaInterface()
        {
            SeaPluginManifest pluginManifest = services.ConfigureSeaPlugins([SeaPluginIdentifiers.Clinic]);
            _ = services.AddSingleton(pluginManifest);
            _ = services.AddSingleton<ILoadedPluginCatalog, SeaPluginCatalog>();
            _ = services.AddScoped<IProjectGridCellDisplayPlugin, DefaultProjectGridCellDisplayPlugin>();
            _ = services.AddScoped<IEventItemPlugin, AttachmentPlugin>();
            _ = services.AddScoped<IEventItemPlugin, ReminderPlugin>();
            return services;
        }

    }
}
