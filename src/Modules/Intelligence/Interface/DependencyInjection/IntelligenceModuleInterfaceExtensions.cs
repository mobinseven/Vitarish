using Microsoft.Extensions.DependencyInjection;
using Vitarish.Intelligence.Interface.Services;

namespace Vitarish.Intelligence.Interface.DependencyInjection;

public static class IntelligenceModuleInterfaceExtensions
{
    public static IServiceCollection AddIntelligenceModuleComponents(this IServiceCollection services)
    {
        _ = services.AddScoped<IStreamResponder, StreamResponder>();
        _ = services.AddScoped<IStreamRewriter, StreamRewriter>();
        _ = services.AddScoped<IStreamTranscriber, StreamTranscriber>();
        return services;
    }
}
