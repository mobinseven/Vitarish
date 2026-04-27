namespace Vitarish.Reminders.Interface.DependencyInjection;

public static class InterfaceConfigurationExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection ConfigureRemindersInterface()
        {
            _ = services.AddVitarishRemindersInterface();
            return services;
        }
    }
}
