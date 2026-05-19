namespace Vitarish.Clinic.Interface.DependencyInjection;

public static class InterfaceConfigurationExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection ConfigureMedicalInterface()
        {
            _ = services.ConfigureMedicalPlugins();
            return services;
        }
    }
}
