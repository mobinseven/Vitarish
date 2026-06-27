using Microsoft.Extensions.DependencyInjection;
using Vitarish.Modules.Base.Client.DependencyInjection;
using Vitarish.Social.Client;
using Vitarish.Social.Contracts.Clients;

namespace Vitarish.Social.Interface.DependencyInjection;

public static class SocialInterfaceExtensions
{
    public static IServiceCollection AddSocialInterface(this IServiceCollection services)
    {
        _ = services.AddInternalModuleApiClient<ISocialClient, SocialHttpClient, ISocialConnectionsClient, ISocialWebhooksClient>();
        return services;
    }
}
