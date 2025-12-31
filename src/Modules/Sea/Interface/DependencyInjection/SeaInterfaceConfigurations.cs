using Vitarish.Sea.Interface.Components.Attachments;
using Vitarish.Sea.Interface.Components.Reminders;
using Vitarish.Sea.Interface.Internal;

namespace Vitarish.Sea.Interface.DependencyInjection;

internal static class SeaInterfaceConfigurations
{
    extension(IServiceCollection services)
    {
        public IServiceCollection ConfigureSeaModule()
        {
            SeaPluginManifest pluginManifest = services.ConfigureSeaPlugins([SeaPluginIdentifiers.Clinic]);
            _ = services.AddSingleton(pluginManifest);
            _ = services.AddSingleton<ILoadedPluginCatalog, SeaPluginCatalog>();
            _ = services.AddScoped<IProjectGridCellDisplayPlugin, DefaultProjectGridCellDisplayPlugin>();
            _ = services.AddScoped<IEventItemPlugin, AttachmentPlugin>();
            _ = services.AddScoped<IEventItemPlugin, ReminderPlugin>();
            _ = services.AddScoped<ISeaUsersService, SeaUsersService>();
            _ = services.AddScoped<ISeaUsersService, SeaUsersService>();
            return services;
        }

    }
}
