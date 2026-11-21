using Vitarish.Intelligence.Application.Agents;

namespace Vitarish.E2e.Infrastructure.Events;

/// <summary>
/// Reports every agent conversation route as streaming-enabled, as an app would after enabling its agent
/// profile's streaming capability. Production routes stay disabled until rollout; the E2E host enables them
/// so the streamed turn transport is proven through the browser. The wrapped registry still owns routing.
/// </summary>
internal sealed class E2eStreamingAgentConversationEndpointRegistry(IAgentConversationEndpointRegistry inner) : IAgentConversationEndpointRegistry
{
    public IReadOnlyCollection<AgentConversationRoute> Routes => inner.Routes.Select(Enabled).ToArray();

    public IAgentConversationEndpoint? Resolve(string agentKey, string subjectKind) => inner.Resolve(agentKey, subjectKind);

    public AgentConversationRegistration? ResolveRegistration(string agentKey, string subjectKind) =>
        inner.ResolveRegistration(agentKey, subjectKind) is { } registration
            ? registration with { Route = Enabled(registration.Route) }
            : null;

    private static AgentConversationRoute Enabled(AgentConversationRoute route) => route with { StreamingEnabled = true };
}
