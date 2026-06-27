using Vitarish.Communications.Client;
using Vitarish.Modules.Aggregator.Core.DependencyInjection;

namespace Vitarish.Communications.Interface.DependencyInjection;

public static class InterfaceConfigurationExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection ConfigureCommunicationsInterface()
        {
            _ = services.AddVitarishCommunicationsInterface();
            _ = services.AddInternalModuleApiClient<ICommunicationsClient, CommunicationsClient>();
            return services;
        }
    }
}
