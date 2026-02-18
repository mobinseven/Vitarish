using Vitarish.Interfaces.Base.Plugins;
using Vitarish.Sea.Interface.Components.Feed;

namespace Vitarish.Sea.Interface.DependencyInjection;

public static class SeaInterfaceConfigurations
{
    extension(IServiceCollection services)
    {
        public IServiceCollection ConfigureSeaInterface()
        {
            _ = services.ConfigureSeaPlugins();
            _ = services.AddScoped<IEventItemPlugin, AttachmentPlugin>();
            _ = services.AddScoped<IEventItemPlugin, ReminderPlugin>();
            _ = services.AddScoped<IFeedPlugin, SeaProjectsFeedPlugin>();
            _ = services.AddScoped<IFeedPlugin, SeaAttachmentsFeedPlugin>();
            _ = services.AddVitarishSeaInterface();
            return services;
        }

    }
}
