using Vitarish.Clinic.Client;
using Vitarish.Modules.Base.Client.DependencyInjection;

namespace Vitarish.Clinic.Interface.DependencyInjection;

public static class InterfaceConfigurationExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection ConfigureMedicalInterface()
        {
            _ = services.ConfigureMedicalPlugins();
            _ = services.AddInternalModuleApiClient<IClinicClient, ClinicClient>();
            return services;
        }
    }
}
