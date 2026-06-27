using Microsoft.Extensions.DependencyInjection;
using Vitarish.Feed.Client;
using Vitarish.Modules.Base.Client.DependencyInjection;

namespace Vitarish.Feed.Interface.DependencyInjection;

public static class FeedInterfaceExtensions
{
    public static IServiceCollection AddFeedInterface(this IServiceCollection services)
    {
        _ = services.AddInternalModuleApiClient<IFeedClient, FeedHttpClient>();
        return services;
    }
}
