namespace Vitarish.Clinic.Application.ClinicWorkspace;

public enum ClinicWorkspaceActionStatus
{
    Proposed,
    Confirming,
    Completed,
    Rejected,
    Expired,
    Failed
}

public sealed record ClinicWorkspaceAction(
    Guid ActionId,
    string AgentRunId,
    string ConversationId,
    Guid PatientId,
    string PracticeId,
    string RequestedByUserId,
    string Capability,
    string ActionType,
    Guid OwningProposalId,
    string ReviewPayload,
    ClinicWorkspaceActionStatus Status,
    DateTimeOffset PreparedAt,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? ConfirmedAt = null,
    string? ConfirmedByUserId = null,
    DateTimeOffset? RejectedAt = null,
    string? RejectionReason = null,
    ClinicWorkspaceActionFailure? Failure = null,
    string? CompletionPayload = null,
    string Summary = "",
    string SubjectKind = "patient",
    string? ToolCallId = null,
    string? ToolName = null,
    int ActionPolicyRevision = 0);

public sealed record ClinicWorkspaceActionFailure(
    string Code,
    string Message,
    bool Retryable);
