using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Clients.Identity.DependencyInjection;
using Vitarish.Clients.Modules.Intelligence.Abstractions;
using Vitarish.Clients.Modules.Intelligence.Internal.Services;

namespace Vitarish.Clients.Modules.Intelligence.DependencyInjection;

public static class IntelligenceModuleExtensions
{
    public static IServiceCollection AddIntelligenceModuleClient(this IServiceCollection services)
    {
        _ = services.AddScoped<IResponderClient, ResponderClient>();
        _ = services.AddScoped<ITranscriberClient, TranscriberClient>();
        _ = services.AddScoped<IModelRegistry, ModelRegistry>();
        _ = services.AddIdentitySignalRClient<TranscriberClientDefinition>();
        _ = services.AddIdentitySignalRClient<ResponderClientDefinition>();
        return services;
    }

    public static WebApplicationBuilder AddIntelligenceModuleClient(this WebApplicationBuilder builder)
    {
        _ = builder.Services.AddIntelligenceModuleClient();
        return builder;
    }
}
