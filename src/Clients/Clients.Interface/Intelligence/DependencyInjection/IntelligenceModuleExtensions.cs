using Vitarish.Clients.Interface.Intelligence.Abstractions;
using Vitarish.Clients.Interface.Intelligence.Services;

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
