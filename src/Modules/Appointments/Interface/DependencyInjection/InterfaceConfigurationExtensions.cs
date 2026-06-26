using Vitarish.Appointments.Client;
using Vitarish.Modules.Base.DependencyInjection;

namespace Vitarish.Appointments.Interface.DependencyInjection;

public static class InterfaceConfigurationExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection ConfigureAppointmentsInterface()
        {
            _ = services.ConfigureAppointmentPlugins();
            _ = services.AddVitarishAppointmentsInterface();
            _ = services.AddInternalModuleApiClient<IAppointmentsClient, AppointmentsClient>();
            return services;
        }
    }
}
