namespace Vitarish.Communications.Interface.DependencyInjection;

public static class InterfaceConfigurationExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection ConfigureCommunicationsInterface()
        {
            _ = services.AddVitarishCommunicationsInterface();
            return services;
        }
    }
}
