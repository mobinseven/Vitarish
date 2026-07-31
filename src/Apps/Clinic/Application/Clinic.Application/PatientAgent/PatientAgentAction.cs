namespace Vitarish.Clinic.Application.PatientAgent;

public enum PatientAgentActionStatus
{
    Proposed,
    Confirming,
    Completed,
    Rejected,
    Expired,
    Failed
}

public sealed record PatientAgentAction(
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
    PatientAgentActionStatus Status,
    DateTimeOffset PreparedAt,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? ConfirmedAt = null,
    string? ConfirmedByUserId = null,
    DateTimeOffset? RejectedAt = null,
    string? RejectionReason = null,
    PatientAgentActionFailure? Failure = null,
    string? CompletionPayload = null,
    string Summary = "",
    string SubjectKind = "patient",
    string? ToolCallId = null,
    string? ToolName = null);

public sealed record PatientAgentActionFailure(
    string Code,
    string Message,
    bool Retryable);
