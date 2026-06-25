using Microsoft.Extensions.DependencyInjection;
using Vitarish.Modules.Base.DependencyInjection;
using Vitarish.Parties.Client;
namespace Vitarish.Parties.Interface.DependencyInjection;
public static class PartiesInterfaceExtensions
{
    public static IServiceCollection ConfigurePartiesInterface(this IServiceCollection services)
    {
        _ = services.AddInternalModuleApiClient<IPartiesClient, PartiesClient>();
        return services;
    }
}
