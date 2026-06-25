using Microsoft.Extensions.DependencyInjection;
using Vitarish.Search.Client;

namespace Vitarish.Search.Interface.DependencyInjection;

public static class SearchInterfaceExtensions
{
    public static IServiceCollection ConfigureSearchInterface(this IServiceCollection services)
    {
        _ = services.AddScoped<ISearchClient, SearchClient>();
        return services;
    }
}
