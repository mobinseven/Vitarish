namespace Vitarish.Appointments.Interface.DependencyInjection;

public static class InterfaceConfigurationExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection ConfigureAppointmentsInterface()
        {
            _ = services.ConfigureAppointmentPlugins();
            _ = services.AddVitarishAppointmentsInterface();
            return services;
        }
    }
}
