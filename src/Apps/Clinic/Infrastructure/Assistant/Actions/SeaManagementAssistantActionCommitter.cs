using System.Text.Json;
using Vitarish.Clinic.Application.Assistant;
using Vitarish.Clinic.Application.Assistant.Actions;
using Vitarish.Intelligence.Application.Agents;
using Vitarish.Sea.Application.Agents;

namespace Vitarish.Clinic.Infrastructure.Assistant.Actions;

/// <summary>Compatibility adapter; SEA owns parsing and confirmed execution.</summary>
public sealed class SeaManagementAssistantActionCommitter(ISeaManagementAgentTool tool) : IAssistantActionCommitter
{
    public string ActionType => "sea_manage";
    public string WorkflowKey => "sea-management";

    public async Task<AssistantActionCommitResult> CommitAsync(AssistantAction action, AssistantContext context, CancellationToken cancellationToken)
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
                ? AssistantActionCommitResult.Failed("unknown_commit_outcome", "The SEA action returned an unknown outcome.")
                : AssistantActionCommitResult.Completed(result.Output);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (JsonException exception) { return AssistantActionCommitResult.Failed("invalid_sea_action", exception.Message); }
        catch (Exception) { return AssistantActionCommitResult.Failed("unknown_commit_outcome", "The SEA action returned an unknown outcome."); }
    }
}
