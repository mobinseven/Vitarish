using Vitarish.Intelligence.Application.Agents;

namespace Vitarish.Clinic.Application.ClinicWorkspace;

/// <summary>Application-owned boundary for running a trusted Clinic workspace.</summary>
public interface IClinicWorkspaceRuntime
{
    Task<ClinicWorkspaceResult> RunAsync(
        ClinicWorkspaceContext context,
        ClinicWorkspaceInput input,
        CancellationToken cancellationToken = default);
}

/// <summary>Application-owned boundary for re-resolving a guarded runtime callback tool.</summary>
public interface IClinicWorkspaceGuardedToolResolver
{
    Task<ClinicWorkspaceToolResult> ResolveAsync(
        ClinicWorkspaceToolResolutionRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record ClinicWorkspaceToolResolutionRequest(
    ClinicWorkspaceContext Context,
    string ExecutionId,
    IReadOnlyDictionary<string, string> ExecutionMetadata,
    string ToolCallId,
    string ToolName,
    string ArgumentsJson);

public sealed record ClinicWorkspaceToolResult(
    string CallId,
    string Output,
    bool IsError = false);

public sealed record ClinicWorkspacePreparedAction(
    string Capability,
    string ActionType,
    Guid OwningId,
    string ReviewDataJson,
    DateTimeOffset? Expiration,
    string Summary = "",
    string? ToolCallId = null,
    string? ToolName = null,
    string? AgentRunId = null);

public interface IClinicWorkspacePreparationTool : IAgentTool
{
    ClinicWorkspacePreparedAction? GetPreparedAction(
        AgentToolResult result);
}
