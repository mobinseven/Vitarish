using System.Text.Json;
using System.Text.Json.Nodes;
using Vitarish.Clinic.Application.Assistant;
using Vitarish.Clinic.Application.Assistant.Actions;
using Vitarish.Clinic.Infrastructure.Assistant.Capabilities.Operations;
using Vitarish.Intelligence.Application.Agents;

namespace Vitarish.Clinic.Infrastructure.Assistant.Actions;

public sealed class AppointmentAdministrationAssistantActionCommitter(AppointmentAdministrationAgentTool tool) : IAssistantActionCommitter
{
    public string ActionType => "appointment_manage";
    public string WorkflowKey => "appointment-management";

    public async Task<AssistantActionCommitResult> CommitAsync(AssistantAction action, AssistantContext context, CancellationToken cancellationToken)
    {
        try
        {
            JsonObject arguments = JsonNode.Parse(action.ReviewPayload)?.AsObject()
                ?? throw new JsonException("The appointment action payload is invalid.");
            arguments["commit"] = true;
            AgentToolResult result = await tool.ExecuteAsync(
                new AgentToolCall(action.ToolCallId ?? action.ActionId.ToString("N"), "appointment_manage", arguments.ToJsonString()),
                new AgentExecutionContext(action.AgentRunId, new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["confirmedAction"] = "true"
                }),
                cancellationToken);
            return result.IsError
                ? AssistantActionCommitResult.Failed("appointment_mutation_failed", "The appointment operation could not be completed.")
                : AssistantActionCommitResult.Completed(result.Output);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (JsonException ex)
        {
            return AssistantActionCommitResult.Failed("invalid_appointment_action", ex.Message);
        }
        catch (Exception ex)
        {
            return AssistantActionCommitResult.Failed("appointment_mutation_failed", ex.Message);
        }
    }
}
