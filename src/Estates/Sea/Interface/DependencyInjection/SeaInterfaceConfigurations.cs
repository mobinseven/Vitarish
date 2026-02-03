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
            _ = services.AddVitarishSeaInterface();
            return services;
        }

    }
}
