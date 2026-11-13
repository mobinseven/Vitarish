namespace Vitarish.Intelligence.Domain.PreparedActions;

/// <summary>Where a prepared action is in its life.</summary>
public enum PreparedActionStatus
{
    Proposed,
    Confirming,
    Completed,
    Rejected,
    Expired,
    Failed
}

/// <summary>Why carrying out a prepared action failed, and whether confirming it again may succeed.</summary>
public sealed record PreparedActionFailure(string Code, string Message, bool Retryable);

/// <summary>
/// A change an assistant tool proposed, stored with the payload a person reviews. It is app-neutral: the owning
/// agent, the app's scope and an opaque subject say whose it is; the review and completion payloads are opaque.
/// A prepared action never changes its proposal; it only moves between statuses through the transitions below,
/// each of which returns the next record, or null when the transition is not allowed from the current status.
/// </summary>
public sealed record PreparedAction(
    Guid ActionId,
    string AgentKey,
    string ScopeId,
    string SubjectKind,
    string SubjectId,
    string ConversationId,
    string AgentRunId,
    string RequestedByUserId,
    string Capability,
    string ActionType,
    int PolicyRevision,
    string ReviewPayload,
    string Summary,
    string? ToolCallId,
    string? ToolName,
    PreparedActionStatus Status,
    DateTimeOffset PreparedAt,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? ConfirmedAt = null,
    string? ConfirmedByUserId = null,
    DateTimeOffset? RejectedAt = null,
    string? RejectionReason = null,
    PreparedActionFailure? Failure = null,
    string? CompletionPayload = null)
{
    /// <summary>Claims the action for confirmation: from Proposed, or from a retryable failure when retrying.</summary>
    public PreparedAction? BeginConfirmation(bool retryFailed) =>
        Status == PreparedActionStatus.Proposed ||
        retryFailed && Status == PreparedActionStatus.Failed && Failure?.Retryable == true
            ? this with { Status = PreparedActionStatus.Confirming, Failure = null }
            : null;

    /// <summary>Records that the owning workflow carried the action out.</summary>
    public PreparedAction? Complete(DateTimeOffset confirmedAt, string confirmedByUserId, string completionPayload) =>
        Status == PreparedActionStatus.Confirming
            ? this with
            {
                Status = PreparedActionStatus.Completed,
                ConfirmedAt = confirmedAt,
                ConfirmedByUserId = confirmedByUserId,
                CompletionPayload = completionPayload
            }
            : null;

    /// <summary>Records a failure before or during confirmation.</summary>
    public PreparedAction? Fail(PreparedActionFailure failure) =>
        Status is PreparedActionStatus.Proposed or PreparedActionStatus.Confirming
            ? this with { Status = PreparedActionStatus.Failed, Failure = failure }
            : null;

    /// <summary>Records that the person declined the proposal.</summary>
    public PreparedAction? Reject(DateTimeOffset rejectedAt, string? reason) =>
        Status == PreparedActionStatus.Proposed
            ? this with { Status = PreparedActionStatus.Rejected, RejectedAt = rejectedAt, RejectionReason = reason }
            : null;

    /// <summary>Records that the proposal outlived its lifetime before it was carried out.</summary>
    public PreparedAction? Expire() =>
        Status is PreparedActionStatus.Proposed or PreparedActionStatus.Failed
            ? this with { Status = PreparedActionStatus.Expired }
            : null;

    public bool IsExpiredAt(DateTimeOffset now) => ExpiresAt <= now;
}
