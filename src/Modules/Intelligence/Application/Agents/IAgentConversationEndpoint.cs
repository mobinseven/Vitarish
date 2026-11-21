using System.Text.Json;
using Vitarish.Intelligence.Application.GuidedSurfaces;

namespace Vitarish.Intelligence.Application.Agents;

public sealed record AgentConversationEndpointRequest(
    string AgentKey,
    string SubjectKind,
    string SubjectId,
    string Message,
    string? ConversationId,
    IAgentTurnObserver? Observer = null);

public sealed record AgentConversationEndpointResult(
    AgentConversationOutcome Outcome,
    string? AgentRunId = null,
    Guid? ConversationId = null,
    JsonElement? Result = null,
    IReadOnlyList<GuidedSurfaceBlock>? Blocks = null);

/// <summary>
/// One agent route and its streaming capability. <paramref name="StreamingEnabled"/> is off unless the
/// owning app registers it on, and is the one-agent-at-a-time enablement and rollback switch for the
/// streamed turn transport. An endpoint registering it on must pass the request's observer to the
/// conversation workflow.
/// </summary>
public sealed record AgentConversationRoute(string AgentKey, string SubjectKind, bool StreamingEnabled = false)
{
    public bool Matches(string agentKey, string subjectKind) =>
        string.Equals(AgentKey, agentKey, StringComparison.Ordinal) &&
        string.Equals(SubjectKind, subjectKind, StringComparison.Ordinal);
}

public interface IAgentConversationEndpoint
{
    IReadOnlyCollection<AgentConversationRoute> Routes { get; }

    Task<AgentConversationEndpointResult> RunAsync(
        AgentConversationEndpointRequest request,
        CancellationToken cancellationToken);
}

public sealed record AgentConversationRegistration(AgentConversationRoute Route, IAgentConversationEndpoint Endpoint);

/// <summary>Resolves the one app-owned conversation endpoint for an explicit agent route.</summary>
public interface IAgentConversationEndpointRegistry
{
    IReadOnlyCollection<AgentConversationRoute> Routes { get; }

    IAgentConversationEndpoint? Resolve(string agentKey, string subjectKind);

    AgentConversationRegistration? ResolveRegistration(string agentKey, string subjectKind);
}

public sealed class AgentConversationEndpointRegistry : IAgentConversationEndpointRegistry
{
    private readonly IReadOnlyList<(AgentConversationRoute Route, IAgentConversationEndpoint Endpoint)> _endpoints;

    public AgentConversationEndpointRegistry(IEnumerable<IAgentConversationEndpoint> endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        List<(AgentConversationRoute Route, IAgentConversationEndpoint Endpoint)> registered = [];
        foreach (IAgentConversationEndpoint endpoint in endpoints)
        {
            foreach (AgentConversationRoute route in endpoint.Routes)
            {
                if (string.IsNullOrWhiteSpace(route.AgentKey) || string.IsNullOrWhiteSpace(route.SubjectKind))
                    throw new InvalidOperationException("Agent conversation routes require an agent key and subject kind.");
                if (registered.Any(existing => existing.Route.Matches(route.AgentKey, route.SubjectKind)))
                    throw new InvalidOperationException($"The agent conversation route '{route.AgentKey}/{route.SubjectKind}' has multiple owners.");
                registered.Add((route, endpoint));
            }
        }
        _endpoints = registered;
    }

    public IReadOnlyCollection<AgentConversationRoute> Routes => _endpoints.Select(pair => pair.Route).ToArray();

    public IAgentConversationEndpoint? Resolve(string agentKey, string subjectKind) =>
        ResolveRegistration(agentKey, subjectKind)?.Endpoint;

    public AgentConversationRegistration? ResolveRegistration(string agentKey, string subjectKind) =>
        _endpoints.Where(pair => pair.Route.Matches(agentKey, subjectKind))
            .Select(pair => new AgentConversationRegistration(pair.Route, pair.Endpoint))
            .FirstOrDefault();
}

public sealed record AgentRuntimeToolEndpointRequest(
    AgentToolCall ToolCall,
    AgentExecutionContext ExecutionContext,
    AgentRuntimeExecutionEnvelope Envelope);

/// <summary>App-owned executor for signed remote tool calls associated with an agent key.</summary>
public interface IAgentRuntimeToolEndpoint
{
    IReadOnlyCollection<string> AgentKeys { get; }
    Task<AgentToolResult> ExecuteAsync(AgentRuntimeToolEndpointRequest request, CancellationToken cancellationToken);
}

public interface IAgentRuntimeToolEndpointRegistry
{
    IAgentRuntimeToolEndpoint? Resolve(string agentKey);
}

public sealed class AgentRuntimeToolEndpointRegistry : IAgentRuntimeToolEndpointRegistry
{
    private readonly IReadOnlyDictionary<string, IAgentRuntimeToolEndpoint> endpoints;

    public AgentRuntimeToolEndpointRegistry(IEnumerable<IAgentRuntimeToolEndpoint> values)
    {
        Dictionary<string, IAgentRuntimeToolEndpoint> registered = new(StringComparer.Ordinal);
        foreach (IAgentRuntimeToolEndpoint endpoint in values)
        foreach (string key in endpoint.AgentKeys)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new InvalidOperationException("Agent runtime tool routes require an agent key.");
            if (!registered.TryAdd(key, endpoint)) throw new InvalidOperationException($"The agent runtime tool route '{key}' has multiple owners.");
        }
        endpoints = registered;
    }

    public IAgentRuntimeToolEndpoint? Resolve(string agentKey) => endpoints.GetValueOrDefault(agentKey);
}
