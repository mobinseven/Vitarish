using System.Text.Json;
using Vitarish.Clinic.Application.PatientAgent;
using Vitarish.Clinic.Application.PatientAgent.Actions;
using Vitarish.Intelligence.Application.Agents;
using Vitarish.Sea.Application.Agents;

namespace Vitarish.Clinic.Infrastructure.PatientAgent.Actions;

/// <summary>Compatibility adapter; SEA owns parsing and confirmed execution.</summary>
public sealed class SeaManagementPatientAgentActionCommitter(ISeaManagementAgentTool tool) : IPatientAgentActionCommitter
{
    public string ActionType => "sea_manage";

    public async Task<PatientAgentActionCommitResult> CommitAsync(PatientAgentAction action, PatientAgentContext context, CancellationToken cancellationToken)
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
                ? PatientAgentActionCommitResult.Failed("sea_mutation_failed", "The SEA action could not be completed.", true)
                : PatientAgentActionCommitResult.Completed(result.Output);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (JsonException exception) { return PatientAgentActionCommitResult.Failed("invalid_sea_action", exception.Message); }
        catch (Exception exception) { return PatientAgentActionCommitResult.Failed("sea_mutation_failed", exception.Message, true); }
    }
}
