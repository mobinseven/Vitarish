using Microsoft.Extensions.DependencyInjection;
using Vitarish.Clinic.Application.Assistant;
using Vitarish.Clinic.Application.Assistant.Capabilities;
using Vitarish.Intelligence.Agents;

namespace Vitarish.Clinic.Agents;

public sealed class AssistantRuntime(
    AssistantConversationRunner conversationRunner,
    AssistantToolResolver toolResolver) : IAssistantRuntime, IAssistantGuardedToolResolver
{
    public const string ResultSchema = """
        {
          "type":"object",
          "additionalProperties":false,
          "required":["messages","actions"],
          "properties":{
            "messages":{"type":"array","items":{"type":"object","additionalProperties":false,"required":["kind","content"],"properties":{"kind":{"type":"string","enum":["assistantMessage","informationalToolResult","missingInformationRequest","failure"]},"content":{"type":"string"}}}},
            "actions":{"type":"array","items":{"type":"object","additionalProperties":false,"required":["capability","actionType","summary"],"properties":{"capability":{"type":"string"},"actionType":{"type":"string"},"summary":{"type":"string"}}}}
          }
        }
        """;

    public Task<AssistantResult> RunAsync(
        AssistantContext context,
        AssistantInput input,
        CancellationToken cancellationToken = default) =>
        conversationRunner.RunAsync(context, input, cancellationToken);

    public Task<AssistantToolResult> ResolveAsync(
        AssistantToolResolutionRequest request,
        CancellationToken cancellationToken = default) =>
        toolResolver.ResolveAsync(request, cancellationToken);
}

public static class AssistantCompositionServiceCollectionExtensions
{
    public static IServiceCollection AddAssistantComposition(this IServiceCollection services)
    {
        // Registered from the profiles' own keys, so a profile never names a capability without a pack.
        _ = services.AddAgentComposition<AssistantCompositionContext>(
            [.. AssistantCapabilityKeys.All.Select(key => (IAgentCapabilityPack<AssistantCompositionContext>)new AssistantCapabilityPack(key))]);
        _ = services.AddTransient<IAssistantCapability, SearchAssistantCapability>();
        _ = services.AddTransient<AssistantExecutionContextFactory>();
        _ = services.AddTransient<AssistantManifestComposer>();
        _ = services.AddTransient<AssistantResultParser>();
        _ = services.AddTransient<AssistantConversationRunner>();
        _ = services.AddTransient<AssistantToolResolver>();
        _ = services.AddTransient<AssistantRuntime>();
        _ = services.AddTransient<IAssistantRuntime>(sp => sp.GetRequiredService<AssistantRuntime>());
        _ = services.AddTransient<IAssistantGuardedToolResolver>(sp => sp.GetRequiredService<AssistantRuntime>());
        return services;
    }
}
