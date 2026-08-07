using System.Text.Json;
using System.Text.Json.Nodes;
using Vitarish.Clinic.Application.PatientAgent;
using Vitarish.Clinic.Application.PatientAgent.Actions;
using Vitarish.Clinic.Infrastructure.PatientAgent.Capabilities.Operations;
using Vitarish.Intelligence.Application.Agents;

namespace Vitarish.Clinic.Infrastructure.PatientAgent.Actions;

public sealed class ReminderManagementPatientAgentActionCommitter(ReminderManagementAgentTool tool) : IPatientAgentActionCommitter
{
    public string ActionType => "reminder_manage";
    public async Task<PatientAgentActionCommitResult> CommitAsync(PatientAgentAction action, PatientAgentContext context, CancellationToken cancellationToken)
    {
        try
        {
            JsonObject arguments = JsonNode.Parse(action.ReviewPayload)?.AsObject() ?? throw new JsonException("The reminder action payload is invalid.");
            arguments["commit"] = true;
            AgentToolResult result = await tool.ExecuteAsync(new AgentToolCall(action.ToolCallId ?? action.ActionId.ToString("N"), ActionType, arguments.ToJsonString()), new AgentExecutionContext(action.AgentRunId, new Dictionary<string, string> { ["confirmedAction"] = "true" }), cancellationToken);
            return result.IsError ? PatientAgentActionCommitResult.Failed("reminder_mutation_failed", "The reminder operation could not be completed.") : PatientAgentActionCommitResult.Completed(result.Output);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (JsonException ex) { return PatientAgentActionCommitResult.Failed("invalid_reminder_action", ex.Message); }
        catch (Exception ex) { return PatientAgentActionCommitResult.Failed("reminder_mutation_failed", ex.Message); }
    }
}
