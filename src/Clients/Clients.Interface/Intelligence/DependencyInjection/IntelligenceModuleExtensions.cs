using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Clients.Interface.Intelligence.Abstractions;
using Vitarish.Clients.Interface.Intelligence.Services;
using Vitarish.Clients.Modules.Intelligence.DependencyInjection;

namespace Vitarish.Clients.Interface.Intelligence.DependencyInjection;

public static class IntelligenceModuleExtensions
{
    public static IServiceCollection AddIntelligenceModuleComponents(this IServiceCollection services)
    {
        _ = services.AddScoped<IStreamResponder, StreamResponder>();
        _ = services.AddScoped<IStreamRewriter, StreamRewriter>();
        _ = services.AddScoped<IStreamTranscriber, StreamTranscriber>();
        return services;
    }

    public static WebApplicationBuilder AddIntelligenceModuleComponents(this WebApplicationBuilder builder)
    {
        _ = builder.Services.AddIntelligenceModuleComponents();
        return builder;
    }
}
