using Microsoft.Extensions.DependencyInjection;
using Vitarish.Intelligence.Client;
using Vitarish.Intelligence.Interface.Services;
using Vitarish.Modules.Base.DependencyInjection;

namespace Vitarish.Intelligence.Interface.DependencyInjection;

public static class IntelligenceInterfaceExtensions
{
    public static IServiceCollection ConfigureIntelligenceInterface(this IServiceCollection services)
    {
        _ = services.AddInternalModuleApiClient<IIntelligenceClient, IntelligenceClient>();
        _ = services.AddScoped<IStreamResponder, StreamResponder>();
        _ = services.AddScoped<IStreamTranscriber, StreamTranscriber>();
        _ = services.AddScoped<IStreamRewriter, StreamRewriter>();
        return services;
    }
}
