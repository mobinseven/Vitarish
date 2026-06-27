using Microsoft.Extensions.DependencyInjection;
using Vitarish.Modules.Aggregator.Core.DependencyInjection;
using Vitarish.Requests.Client;

namespace Vitarish.Requests.Interface.DependencyInjection;

public static class RequestsInterfaceExtensions
{
    public static IServiceCollection AddRequestsInterface(this IServiceCollection services)
    {
        _ = services.AddInternalModuleApiClient<IRequestsClient, RequestsHttpClient>();
        return services;
    }
}
