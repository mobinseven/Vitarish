using Vitarish.Clients.Interface.Sea.Abstractions;
using Vitarish.Clients.Interface.Sea.Components.Attachments;
using Vitarish.Clients.Interface.Sea.Components.Reminders;
using Vitarish.Clients.Interface.Sea.Internal;

namespace Vitarish.Clients.Interface.Sea;

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
            return services;
        }

    }
}
