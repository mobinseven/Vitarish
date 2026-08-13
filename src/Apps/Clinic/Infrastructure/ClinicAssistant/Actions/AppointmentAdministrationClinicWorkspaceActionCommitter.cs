using System.Text.Json;
using System.Text.Json.Nodes;
using Vitarish.Clinic.Application.ClinicWorkspace;
using Vitarish.Clinic.Application.ClinicWorkspace.Actions;
using Vitarish.Clinic.Infrastructure.ClinicWorkspace.Capabilities.Operations;
using Vitarish.Intelligence.Application.Agents;

namespace Vitarish.Clinic.Infrastructure.ClinicWorkspace.Actions;

public sealed class AppointmentAdministrationClinicWorkspaceActionCommitter(AppointmentAdministrationAgentTool tool) : IClinicWorkspaceActionCommitter
{
    public string ActionType => "appointment_manage";

    public async Task<ClinicWorkspaceActionCommitResult> CommitAsync(ClinicWorkspaceAction action, ClinicWorkspaceContext context, CancellationToken cancellationToken)
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
                ? ClinicWorkspaceActionCommitResult.Failed("appointment_mutation_failed", "The appointment operation could not be completed.")
                : ClinicWorkspaceActionCommitResult.Completed(result.Output);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (JsonException ex)
        {
            return ClinicWorkspaceActionCommitResult.Failed("invalid_appointment_action", ex.Message);
        }
        catch (Exception ex)
        {
            return ClinicWorkspaceActionCommitResult.Failed("appointment_mutation_failed", ex.Message);
        }
    }
}
