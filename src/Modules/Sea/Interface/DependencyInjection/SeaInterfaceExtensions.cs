using Vitarish.Modules.Base.Client.DependencyInjection;

namespace Vitarish.Sea.Interface.DependencyInjection;

public static class SeaInterfaceExtensions
{
    public static IServiceCollection AddSeaInterface(this IServiceCollection services)
    {
        _ = services.AddInternalModuleApiClient<ISeaClient, SeaClient>();
        return services;
    }
}
