using Vitarish.Sea.Interface.Components.Attachments;
using Vitarish.Sea.Interface.Components.Reminders;

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
            return services;
        }

    }
}
