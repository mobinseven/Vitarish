namespace Vitarish.Clinic.Application.Assistant;

public enum AssistantActionStatus
{
    Proposed,
    Confirming,
    Completed,
    Rejected,
    Expired,
    Failed
}

public sealed record AssistantAction(
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
    AssistantActionStatus Status,
    DateTimeOffset PreparedAt,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? ConfirmedAt = null,
    string? ConfirmedByUserId = null,
    DateTimeOffset? RejectedAt = null,
    string? RejectionReason = null,
    AssistantActionFailure? Failure = null,
    string? CompletionPayload = null,
    string Summary = "",
    string SubjectKind = "patient",
    string? ToolCallId = null,
    string? ToolName = null,
    int ActionPolicyRevision = 0);

public sealed record AssistantActionFailure(
    string Code,
    string Message,
    bool Retryable);
