using Microsoft.Extensions.DependencyInjection;
using Vitarish.Modules.Base.Client.DependencyInjection;
using Vitarish.Search.Client;

namespace Vitarish.Search.Interface.DependencyInjection;

public static class SearchInterfaceExtensions
{
    public static IServiceCollection ConfigureSearchInterface(this IServiceCollection services)
    {
        _ = services.AddInternalModuleApiClient<ISearchClient, SearchClient>();
        return services;
    }
}
