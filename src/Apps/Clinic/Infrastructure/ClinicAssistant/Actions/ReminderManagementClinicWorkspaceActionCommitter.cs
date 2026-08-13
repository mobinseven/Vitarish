using System.Text.Json;
using System.Text.Json.Nodes;
using Vitarish.Clinic.Application.ClinicWorkspace;
using Vitarish.Clinic.Application.ClinicWorkspace.Actions;
using Vitarish.Clinic.Infrastructure.ClinicWorkspace.Capabilities.Operations;
using Vitarish.Intelligence.Application.Agents;

namespace Vitarish.Clinic.Infrastructure.ClinicWorkspace.Actions;

public sealed class ReminderManagementClinicWorkspaceActionCommitter(ReminderManagementAgentTool tool) : IClinicWorkspaceActionCommitter
{
    public string ActionType => "reminder_manage";
    public async Task<ClinicWorkspaceActionCommitResult> CommitAsync(ClinicWorkspaceAction action, ClinicWorkspaceContext context, CancellationToken cancellationToken)
    {
        try
        {
            JsonObject arguments = JsonNode.Parse(action.ReviewPayload)?.AsObject() ?? throw new JsonException("The reminder action payload is invalid.");
            arguments["commit"] = true;
            AgentToolResult result = await tool.ExecuteAsync(new AgentToolCall(action.ToolCallId ?? action.ActionId.ToString("N"), ActionType, arguments.ToJsonString()), new AgentExecutionContext(action.AgentRunId, new Dictionary<string, string> { ["confirmedAction"] = "true" }), cancellationToken);
            return result.IsError ? ClinicWorkspaceActionCommitResult.Failed("reminder_mutation_failed", "The reminder operation could not be completed.") : ClinicWorkspaceActionCommitResult.Completed(result.Output);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (JsonException ex) { return ClinicWorkspaceActionCommitResult.Failed("invalid_reminder_action", ex.Message); }
        catch (Exception ex) { return ClinicWorkspaceActionCommitResult.Failed("reminder_mutation_failed", ex.Message); }
    }
}
