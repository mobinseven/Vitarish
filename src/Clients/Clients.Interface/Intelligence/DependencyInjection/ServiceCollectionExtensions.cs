using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Clients.Interface.Intelligence.Abstractions;
using Vitarish.Clients.Interface.Intelligence.Services;
using Vitarish.Clients.Modules.Intelligence.DependencyInjection;

namespace Vitarish.Clients.Interface.Intelligence.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddIntelligenceComponents(this IServiceCollection services)
    {
        _ = services.AddIntelligenceModuleClient();
        _ = services.AddScoped<IStreamResponder, StreamResponder>();
        _ = services.AddScoped<IStreamRewriter, StreamRewriter>();
        _ = services.AddScoped<IStreamTranscriber, StreamTranscriber>();
        return services;
    }

    public static WebApplicationBuilder AddIntelligenceComponents(this WebApplicationBuilder builder)
    {
        _ = builder.Services.AddIntelligenceComponents();
        return builder;
    }
}
