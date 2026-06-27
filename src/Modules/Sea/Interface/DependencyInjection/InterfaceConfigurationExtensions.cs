namespace Vitarish.Sea.Interface.DependencyInjection;

public static class InterfaceConfigurationExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection ConfigureInterface()
        {
            _ = services.AddSeaInterface();
            _ = services.ConfigurePlugins();
            _ = services.AddScoped<IEventItemPlugin, AttachmentPlugin>();
            _ = services.AddScoped<IEventItemPlugin, ReminderPlugin>();
            _ = services.AddVitarishSeaInterface();
            return services;
        }

    }
}
