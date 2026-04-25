using Vitarish.Appointments.Interface.Plugins.Default;

namespace Vitarish.Appointments.Interface.DependencyInjection;

public static class PluginConfigurationExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection ConfigureAppointmentPlugins()
        {
            _ = services.AddScoped<IAppointmentCardPlugin, DefaultAppointmentCardPlugin>();
            _ = services.AddScoped<IAppointmentFormPlugin, DefaultAppointmentFormPlugin>();
            return services;
        }
    }
}
