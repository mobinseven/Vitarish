using System.Text.Json;
using System.Text.Json.Nodes;
using Vitarish.Clinic.Application.PatientAgent;
using Vitarish.Clinic.Application.PatientAgent.Actions;
using Vitarish.Clinic.Infrastructure.PatientAgent.Capabilities.Operations;
using Vitarish.Intelligence.Application.Agents;

namespace Vitarish.Clinic.Infrastructure.PatientAgent.Actions;

public sealed class SeaManagementPatientAgentActionCommitter(SeaManagementTool tool) : IPatientAgentActionCommitter
{
    public string ActionType => "sea_manage";

    public async Task<PatientAgentActionCommitResult> CommitAsync(
        PatientAgentAction action,
        PatientAgentContext context,
        CancellationToken cancellationToken)
    {
        try
        {
            JsonObject arguments = JsonNode.Parse(action.ReviewPayload)?.AsObject()
                ?? throw new JsonException("The SEA action payload is invalid.");
            arguments["commit"] = true;

            IReadOnlyDictionary<string, string> metadata = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["authenticatedUserId"] = context.AuthenticatedUserId
            };
            AgentToolResult result = await tool.ExecuteAsync(
                new AgentToolCall(action.ToolCallId ?? action.ActionId.ToString("N"), action.ToolName ?? "sea_manage", arguments.ToJsonString()),
                new AgentExecutionContext(action.AgentRunId, metadata),
                cancellationToken);

            return result.IsError
                ? PatientAgentActionCommitResult.Failed("sea_mutation_failed", "The SEA action could not be completed.", true)
                : PatientAgentActionCommitResult.Completed(result.Output);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (JsonException ex)
        {
            return PatientAgentActionCommitResult.Failed("invalid_sea_action", ex.Message);
        }
        catch (Exception ex)
        {
            return PatientAgentActionCommitResult.Failed("sea_mutation_failed", ex.Message, true);
        }
    }
}
