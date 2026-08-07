using System.Text.Json;
using System.Text.Json.Nodes;
using Vitarish.Clinic.Application.PatientAgent;
using Vitarish.Clinic.Application.PatientAgent.Actions;
using Vitarish.Clinic.Infrastructure.PatientAgent.Capabilities.Operations;
using Vitarish.Intelligence.Application.Agents;

namespace Vitarish.Clinic.Infrastructure.PatientAgent.Actions;

public sealed class AppointmentAdministrationPatientAgentActionCommitter(AppointmentAdministrationAgentTool tool) : IPatientAgentActionCommitter
{
    public string ActionType => "appointment_manage";

    public async Task<PatientAgentActionCommitResult> CommitAsync(PatientAgentAction action, PatientAgentContext context, CancellationToken cancellationToken)
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
                ? PatientAgentActionCommitResult.Failed("appointment_mutation_failed", "The appointment operation could not be completed.", true)
                : PatientAgentActionCommitResult.Completed(result.Output);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (JsonException ex)
        {
            return PatientAgentActionCommitResult.Failed("invalid_appointment_action", ex.Message);
        }
        catch (Exception ex)
        {
            return PatientAgentActionCommitResult.Failed("appointment_mutation_failed", ex.Message, true);
        }
    }
}
