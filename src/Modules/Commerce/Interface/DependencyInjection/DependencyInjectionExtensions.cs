using Microsoft.Extensions.DependencyInjection;
using Vitarish.Commerce.Client;
using Vitarish.Commerce.Contracts;
using Vitarish.Modules.Base.Client.DependencyInjection;

namespace Vitarish.Commerce.Interface.DependencyInjection;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection ConfigureCommerceInterface(this IServiceCollection services)
    {
        _ = services.AddInternalModuleApiClient<ICommerceClient, CommerceClient>();
        return services;
    }
}
