using Microsoft.Extensions.DependencyInjection;

namespace Vitarish.Intelligence.Agents;

public static class AgentCompositionServiceCollectionExtensions
{
    public static IServiceCollection AddAgentComposition<TContext>(
        this IServiceCollection services,
        params IAgentCapabilityPack<TContext>[] packs)
    {
        ArgumentNullException.ThrowIfNull(packs);
        _ = services.AddSingleton(new AgentCapabilityCatalog<TContext>(packs));
        _ = services.AddScoped<IntelligenceAgentToolCatalog>();
        _ = services.AddScoped<AgentProfileComposer<TContext>>();
        return services;
    }
}
