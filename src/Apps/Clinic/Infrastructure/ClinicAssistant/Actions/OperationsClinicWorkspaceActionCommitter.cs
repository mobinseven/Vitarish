using System.Text.Json;
using Vitarish.Appointments.Application.AgentCapabilities;
using Vitarish.Clinic.Application.ClinicWorkspace;
using Vitarish.Clinic.Application.ClinicWorkspace.Actions;

namespace Vitarish.Clinic.Infrastructure.ClinicWorkspace.Actions;

public sealed class OperationsClinicWorkspaceActionCommitter(IAppointmentSchedulingAgentCapability scheduling)
    : IClinicWorkspaceActionCommitter
{
    private static readonly IReadOnlyDictionary<string, AgentSchedulingOperation> Operations = new Dictionary<string, AgentSchedulingOperation>(StringComparer.Ordinal)
    {
        ["book_appointment"] = AgentSchedulingOperation.BookAppointment,
        ["reschedule_appointment"] = AgentSchedulingOperation.RescheduleAppointment,
        ["cancel_appointment"] = AgentSchedulingOperation.CancelAppointment,
        ["reject_appointment"] = AgentSchedulingOperation.RejectAppointment,
        ["create_shift"] = AgentSchedulingOperation.CreateShift,
        ["edit_shift"] = AgentSchedulingOperation.EditShift,
        ["close_shift"] = AgentSchedulingOperation.CloseShift,
        ["cancel_shift"] = AgentSchedulingOperation.CancelShift,
        ["archive_shift"] = AgentSchedulingOperation.ArchiveShift
    };
    public string ActionType => "operations";
    public string WorkflowKey => "appointments.operations";
    public IReadOnlySet<string> ActionTypes { get; } = Operations.Keys.ToHashSet(StringComparer.Ordinal);
    public async Task<ClinicWorkspaceActionCommitResult> CommitAsync(ClinicWorkspaceAction action, ClinicWorkspaceContext context, CancellationToken cancellationToken)
    {
        if (!Operations.TryGetValue(action.ActionType, out AgentSchedulingOperation operation))
            return ClinicWorkspaceActionCommitResult.Failed("unsupported_action", "The scheduling action is not enabled.");
        AgentSchedulingContext schedulingContext = new(action.PracticeId, action.AgentRunId, action.ToolCallId ?? action.ActionId.ToString("N"), action.ToolName ?? action.ActionType, context.AuthenticatedUserId);
        AgentSchedulingResult result = await scheduling.ExecutePreparedAsync(schedulingContext, operation, cancellationToken);
        return result.Status switch
        {
            AgentSchedulingStatus.Succeeded or AgentSchedulingStatus.AlreadyApplied => ClinicWorkspaceActionCommitResult.Completed(JsonSerializer.Serialize(result)),
            AgentSchedulingStatus.Expired => ClinicWorkspaceActionCommitResult.Failed("expired_action", result.Message ?? "The action expired."),
            AgentSchedulingStatus.Conflict => ClinicWorkspaceActionCommitResult.Failed("stale_action", result.Message ?? "The action is stale."),
            AgentSchedulingStatus.UnknownOutcome => ClinicWorkspaceActionCommitResult.Failed("unknown_commit_outcome", result.Message ?? "The scheduling outcome is unknown."),
            _ => ClinicWorkspaceActionCommitResult.Failed("scheduling_commit_failed", result.Message ?? "The scheduling action could not be completed.")
        };
    }
}
