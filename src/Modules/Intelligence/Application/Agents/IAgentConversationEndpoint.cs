using System.Text.Json;

namespace Vitarish.Intelligence.Application.Agents;

public sealed record AgentConversationEndpointRequest(
    string AgentKey,
    string SubjectKind,
    string SubjectId,
    string Message,
    string? ConversationId);

public sealed record AgentConversationEndpointResult(
    AgentConversationOutcome Outcome,
    string? AgentRunId = null,
    Guid? ConversationId = null,
    JsonElement? Result = null);

public sealed record AgentConversationRoute(string AgentKey, string SubjectKind)
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

/// <summary>Resolves the one app-owned conversation endpoint for an explicit agent route.</summary>
public interface IAgentConversationEndpointRegistry
{
    IAgentConversationEndpoint? Resolve(string agentKey, string subjectKind);
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

    public IAgentConversationEndpoint? Resolve(string agentKey, string subjectKind) =>
        _endpoints.FirstOrDefault(pair => pair.Route.Matches(agentKey, subjectKind)).Endpoint;
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
