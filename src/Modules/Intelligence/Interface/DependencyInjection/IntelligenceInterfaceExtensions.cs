using Microsoft.Extensions.DependencyInjection;
using Vitarish.Intelligence.Client;
using Vitarish.Intelligence.Interface.Services;

namespace Vitarish.Intelligence.Interface.DependencyInjection;

public static class IntelligenceInterfaceExtensions
{
    public static IServiceCollection ConfigureIntelligenceInterface(this IServiceCollection services)
    {
        _ = services.AddScoped<IIntelligenceClient, IntelligenceClient>();
        _ = services.AddScoped<IStreamResponder, StreamResponder>();
        _ = services.AddScoped<IStreamTranscriber, StreamTranscriber>();
        _ = services.AddScoped<IStreamRewriter, StreamRewriter>();
        return services;
    }
}
