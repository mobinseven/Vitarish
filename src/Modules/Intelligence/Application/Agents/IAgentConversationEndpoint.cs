using System.Text.Json;

namespace Vitarish.Intelligence.Application.Agents;

public sealed record AgentConversationEndpointRequest(
    string AgentKey,
    string SubjectKind,
    string SubjectId,
    string Message,
    string? ConversationId,
    string? PreviousResponseId);

public sealed record AgentConversationEndpointResult(
    AgentConversationOutcome Outcome,
    string? AgentRunId = null,
    Guid? ConversationId = null,
    JsonElement? Result = null);

public interface IAgentConversationEndpoint
{
    Task<AgentConversationEndpointResult> RunAsync(
        AgentConversationEndpointRequest request,
        CancellationToken cancellationToken);
}
