using System.Text.Json;
using Vitarish.Clinic.Application.ClinicWorkspace;
using Vitarish.Clinic.Application.ClinicWorkspace.Actions;
using Vitarish.Intelligence.Application.Agents;
using Vitarish.Sea.Application.Agents;

namespace Vitarish.Clinic.Infrastructure.ClinicWorkspace.Actions;

/// <summary>Compatibility adapter; SEA owns parsing and confirmed execution.</summary>
public sealed class SeaManagementClinicWorkspaceActionCommitter(ISeaManagementAgentTool tool) : IClinicWorkspaceActionCommitter
{
    public string ActionType => "sea_manage";

    public async Task<ClinicWorkspaceActionCommitResult> CommitAsync(ClinicWorkspaceAction action, ClinicWorkspaceContext context, CancellationToken cancellationToken)
    {
        try
        {
            SeaManagementPreparedAction prepared = JsonSerializer.Deserialize<SeaManagementPreparedAction>(action.ReviewPayload)
                ?? throw new JsonException("The SEA action payload is invalid.");
            AgentToolResult result = await tool.ExecutePreparedAsync(
                new AgentToolCall(action.ToolCallId ?? action.ActionId.ToString("N"), action.ToolName ?? "sea_manage", "{}"),
                prepared,
                new SeaManagementAgentSubject(context.PracticeId, context.AuthenticatedUserId),
                cancellationToken);
            return result.IsError
                ? ClinicWorkspaceActionCommitResult.Failed("unknown_commit_outcome", "The SEA action returned an unknown outcome.")
                : ClinicWorkspaceActionCommitResult.Completed(result.Output);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (JsonException exception) { return ClinicWorkspaceActionCommitResult.Failed("invalid_sea_action", exception.Message); }
        catch (Exception) { return ClinicWorkspaceActionCommitResult.Failed("unknown_commit_outcome", "The SEA action returned an unknown outcome."); }
    }
}
