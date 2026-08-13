using Vitarish.Intelligence.Application.Agents;

namespace Vitarish.Clinic.Application.Assistant;

/// <summary>Application-owned boundary for running a trusted Clinic workspace.</summary>
public interface IAssistantRuntime
{
    Task<AssistantResult> RunAsync(
        AssistantContext context,
        AssistantInput input,
        CancellationToken cancellationToken = default);
}

/// <summary>Application-owned boundary for re-resolving a guarded runtime callback tool.</summary>
public interface IAssistantGuardedToolResolver
{
    Task<AssistantToolResult> ResolveAsync(
        AssistantToolResolutionRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record AssistantToolResolutionRequest(
    AssistantContext Context,
    string ExecutionId,
    IReadOnlyDictionary<string, string> ExecutionMetadata,
    string ToolCallId,
    string ToolName,
    string ArgumentsJson,
    AgentRuntimeExecutionEnvelope ExecutionEnvelope);

public sealed record AssistantToolResult(
    string CallId,
    string Output,
    bool IsError = false);

public sealed record AssistantPreparedAction(
    string Capability,
    string ActionType,
    Guid OwningId,
    string ReviewDataJson,
    DateTimeOffset? Expiration,
    string Summary = "",
    string? ToolCallId = null,
    string? ToolName = null,
    string? AgentRunId = null);

public interface IAssistantPreparationTool : IAgentTool
{
    AssistantPreparedAction? GetPreparedAction(
        AgentToolResult result);
}
