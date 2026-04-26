namespace Vitarish.Appointments.Interface.DependencyInjection;

public static class PluginConfigurationExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection ConfigureAppointmentPlugins() => services;
    }
}
