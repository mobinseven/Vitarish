using System.Text.Json;
using System.Text.Json.Nodes;
using Vitarish.Clinic.Application.Assistant;
using Vitarish.Clinic.Application.Assistant.Actions;
using Vitarish.Clinic.Infrastructure.Assistant.Capabilities.Operations;
using Vitarish.Intelligence.Application.Agents;

namespace Vitarish.Clinic.Infrastructure.Assistant.Actions;

public sealed class ReminderManagementAssistantActionCommitter(ReminderManagementAgentTool tool) : IAssistantActionCommitter
{
    public string ActionType => "reminder_manage";
    public string WorkflowKey => "reminder-management";
    public async Task<AssistantActionCommitResult> CommitAsync(AssistantAction action, AssistantContext context, CancellationToken cancellationToken)
    {
        try
        {
            JsonObject arguments = JsonNode.Parse(action.ReviewPayload)?.AsObject() ?? throw new JsonException("The reminder action payload is invalid.");
            arguments["commit"] = true;
            AgentToolResult result = await tool.ExecuteAsync(new AgentToolCall(action.ToolCallId ?? action.ActionId.ToString("N"), ActionType, arguments.ToJsonString()), new AgentExecutionContext(action.AgentRunId, new Dictionary<string, string> { ["confirmedAction"] = "true" }), cancellationToken);
            return result.IsError ? AssistantActionCommitResult.Failed("reminder_mutation_failed", "The reminder operation could not be completed.") : AssistantActionCommitResult.Completed(result.Output);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (JsonException ex) { return AssistantActionCommitResult.Failed("invalid_reminder_action", ex.Message); }
        catch (Exception ex) { return AssistantActionCommitResult.Failed("reminder_mutation_failed", ex.Message); }
    }
}
