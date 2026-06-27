using Microsoft.Extensions.DependencyInjection;
using Vitarish.Medical.Client;
using Vitarish.Modules.Aggregator.Core.DependencyInjection;

namespace Vitarish.Medical.Interface.DependencyInjection;

public static class MedicalModuleInterfaceExtensions
{
    public static IServiceCollection ConfigureMedicalInterface(this IServiceCollection services)
    {
        _ = services.AddInternalModuleApiClient<IMedicalClient, MedicalClient>();
        return services;
    }
}
