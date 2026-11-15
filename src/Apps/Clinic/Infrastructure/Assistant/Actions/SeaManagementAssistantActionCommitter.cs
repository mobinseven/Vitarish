using System.Text.Json;
using Vitarish.Clinic.Application.Assistant;
using Vitarish.Clinic.Application.Assistant.Actions;
using Vitarish.Clinic.Application.Assistant.Capabilities;
using Vitarish.Clinic.Infrastructure.Assistant.Capabilities.Procedures;
using Vitarish.Intelligence.Application.Agents;
using Vitarish.Sea.Application.Agents;

namespace Vitarish.Clinic.Infrastructure.Assistant.Actions;

/// <summary>Compatibility adapter; SEA owns parsing and confirmed execution.</summary>
public sealed class SeaManagementAssistantActionCommitter(ISeaManagementAgentTool tool) : IAssistantActionCommitter
{
    public const string SeaManagementActionType = "sea_manage";
    public string ActionType => SeaManagementActionType;
    public string WorkflowKey => "sea-management";

    public Task<AssistantActionCommitResult> CommitAsync(AssistantAction action, AssistantContext context, CancellationToken cancellationToken) =>
        SeaPreparedActionExecution.CommitAsync(tool, action, context,
            () => JsonSerializer.Deserialize<SeaManagementPreparedAction>(action.ReviewPayload), cancellationToken);
}

/// <summary>Carries out a confirmed change to a patient's procedure or step; SEA authorizes it again as it runs.</summary>
public sealed class ProcedureChangeActionCommitter(ISeaManagementAgentTool tool) : IAssistantActionCommitter
{
    public string ActionType => ProcedureActionTypes.StepChange;
    public string WorkflowKey => ProcedureActionTypes.Workflow;
    public IReadOnlySet<string> ActionTypes => ProcedureActionTypes.All;

    public Task<AssistantActionCommitResult> CommitAsync(AssistantAction action, AssistantContext context, CancellationToken cancellationToken) =>
        SeaPreparedActionExecution.CommitAsync(tool, action, context,
            () => JsonSerializer.Deserialize(action.ReviewPayload, ProcedureChangeJsonSerializerContext.Default.ProcedureChangePayload)?.Confirmation,
            cancellationToken);
}

internal static class SeaPreparedActionExecution
{
    public static async Task<AssistantActionCommitResult> CommitAsync(
        ISeaManagementAgentTool tool,
        AssistantAction action,
        AssistantContext context,
        Func<SeaManagementPreparedAction?> read,
        CancellationToken cancellationToken)
    {
        try
        {
            SeaManagementPreparedAction prepared = read() ?? throw new JsonException("The SEA action payload is invalid.");
            AgentToolResult result = await tool.ExecutePreparedAsync(
                new AgentToolCall(action.ToolCallId ?? action.ActionId.ToString("N"), action.ToolName ?? tool.Definition.Name, "{}"),
                prepared,
                new SeaManagementAgentSubject(context.PracticeId, context.AuthenticatedUserId),
                cancellationToken);
            return result.IsError
                ? AssistantActionCommitResult.Failed("unknown_commit_outcome", "The SEA action returned an unknown outcome.")
                : AssistantActionCommitResult.Completed(result.Output);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (JsonException exception) { return AssistantActionCommitResult.Failed("invalid_sea_action", exception.Message); }
        catch (UnauthorizedAccessException) { return AssistantActionCommitResult.Failed("not_permitted", "The user's access does not allow this change."); }
        catch (Exception) { return AssistantActionCommitResult.Failed("unknown_commit_outcome", "The SEA action returned an unknown outcome."); }
    }
}
