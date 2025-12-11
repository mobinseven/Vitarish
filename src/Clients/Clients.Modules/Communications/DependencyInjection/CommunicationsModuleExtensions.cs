using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Clients.Identity.DependencyInjection;
using Vitarish.Clients.Modules.Communications.Abstractions;
using Vitarish.Clients.Modules.Communications.Internal;

namespace Vitarish.Clients.Modules.Communications.DependencyInjection;

public static class CommunicationsModuleExtensions
{
    public static IServiceCollection AddCommunicationsModuleClient(this IServiceCollection services)
    {
        _ = services.AddIdentitySignalRClient<MessageClientDefinition>();
        _ = services.AddScoped<IMessageClient, MessageClient>();

        return services;
    }

    public static WebApplicationBuilder AddCommunicationsModuleClient(this WebApplicationBuilder builder)
    {
        _ = builder.Services.AddCommunicationsModuleClient();
        return builder;
    }
}
