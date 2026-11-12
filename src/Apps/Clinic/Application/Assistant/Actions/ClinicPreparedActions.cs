using Vitarish.Clinic.Application.Assistant.Capabilities;
using Vitarish.Intelligence.Application.PreparedActions;
using Vitarish.Intelligence.Domain.PreparedActions;

namespace Vitarish.Clinic.Application.Assistant.Actions;

/// <summary>
/// Clinic's side of the shared prepared-action lifecycle for one authenticated request: the caller comes from the
/// request's assistant context, policy and authorization from the Clinic capability registry, and commits from
/// the Clinic committers, which keep receiving Clinic's view of the action.
/// </summary>
public sealed class ClinicPreparedActionApp(
    AssistantContext context,
    AssistantCapabilityRegistry capabilityRegistry,
    IAssistantActionCommitterRegistry committers) : IPreparedActionApp
{
    public PreparedActionCaller Caller { get; } = new(
        AssistantSubjects.CanonicalAgentKey(context),
        context.PracticeId,
        context.SubjectKind,
        AssistantSubjects.SubjectId(context),
        context.AuthenticatedUserId);

    public PreparedActionPolicy? ResolvePolicy(string actionType) =>
        capabilityRegistry.ResolveAction(actionType) is { } policy
            ? new(policy.Revision, ClinicPreparedActionMapping.ToShared(policy.RetryClassification))
            : null;

    public bool Authorizes(string actionType) =>
        capabilityRegistry.ResolveAction(actionType)?.Authorizes(context) == true;

    public bool CanCommit(string actionType) => committers.Resolve(actionType) is not null;

    public async Task<PreparedActionCommitResult> CommitAsync(PreparedAction action, CancellationToken cancellationToken)
    {
        IAssistantActionCommitter committer = committers.Resolve(action.ActionType)
            ?? throw new InvalidOperationException("The action type has no committer.");
        AssistantActionCommitResult result = await committer.CommitAsync(
            ClinicPreparedActionMapping.ToClinic(action), context, cancellationToken);
        return new(
            result.Succeeded,
            result.CompletionPayload,
            result.Failure is { } failure ? new(failure.Code, failure.Message, failure.Retryable) : null);
    }
}

/// <summary>The one mapping between the shared prepared action and Clinic's view of it.</summary>
public static class ClinicPreparedActionMapping
{
    public static PreparedAction ToPrepared(
        AssistantContext context,
        AssistantPreparedAction prepared,
        string agentRunId,
        string conversationId,
        DateTimeOffset preparedAt,
        DateTimeOffset expiresAt,
        int policyRevision) =>
        new(
            prepared.OwningId,
            AssistantSubjects.CanonicalAgentKey(context),
            context.PracticeId,
            context.SubjectKind,
            AssistantSubjects.SubjectId(context),
            conversationId,
            agentRunId,
            context.AuthenticatedUserId,
            prepared.Capability,
            prepared.ActionType,
            policyRevision,
            prepared.ReviewDataJson,
            prepared.Summary,
            prepared.ToolCallId,
            prepared.ToolName,
            PreparedActionStatus.Proposed,
            preparedAt,
            expiresAt);

    public static AssistantAction ToClinic(PreparedAction action) =>
        new(
            action.ActionId,
            action.AgentRunId,
            action.ConversationId,
            action.SubjectKind == AssistantSubjects.PatientKind && Guid.TryParse(action.SubjectId, out Guid patientId) ? patientId : Guid.Empty,
            action.ScopeId,
            action.RequestedByUserId,
            action.Capability,
            action.ActionType,
            action.ActionId,
            action.ReviewPayload,
            ToClinic(action.Status),
            action.PreparedAt,
            action.ExpiresAt,
            action.ConfirmedAt,
            action.ConfirmedByUserId,
            action.RejectedAt,
            action.RejectionReason,
            action.Failure is { } failure ? new(failure.Code, failure.Message, failure.Retryable) : null,
            action.CompletionPayload,
            action.Summary,
            action.SubjectKind,
            action.ToolCallId,
            action.ToolName,
            action.PolicyRevision);

    public static AssistantActionStatus ToClinic(PreparedActionStatus status) => status switch
    {
        PreparedActionStatus.Proposed => AssistantActionStatus.Proposed,
        PreparedActionStatus.Confirming => AssistantActionStatus.Confirming,
        PreparedActionStatus.Completed => AssistantActionStatus.Completed,
        PreparedActionStatus.Rejected => AssistantActionStatus.Rejected,
        PreparedActionStatus.Expired => AssistantActionStatus.Expired,
        PreparedActionStatus.Failed => AssistantActionStatus.Failed,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
    };

    public static AssistantActionResult ToClinic(PreparedActionResult result) =>
        new(ToClinic(result.Outcome), result.Action is { } action ? ToClinic(action) : null);

    public static AssistantActionOutcome ToClinic(PreparedActionOutcome outcome) => outcome switch
    {
        PreparedActionOutcome.Succeeded => AssistantActionOutcome.Succeeded,
        PreparedActionOutcome.ValidationFailure => AssistantActionOutcome.ValidationFailure,
        PreparedActionOutcome.NotFound => AssistantActionOutcome.NotFound,
        PreparedActionOutcome.ScopeMismatch => AssistantActionOutcome.ScopeMismatch,
        PreparedActionOutcome.PermissionDenied => AssistantActionOutcome.PermissionDenied,
        PreparedActionOutcome.Expired => AssistantActionOutcome.Expired,
        PreparedActionOutcome.Conflict => AssistantActionOutcome.Conflict,
        PreparedActionOutcome.Failed => AssistantActionOutcome.Failed,
        _ => throw new ArgumentOutOfRangeException(nameof(outcome), outcome, null)
    };

    public static PreparedActionRetryClassification ToShared(AssistantActionRetryClassification classification) => classification switch
    {
        AssistantActionRetryClassification.NeverRetry => PreparedActionRetryClassification.NeverRetry,
        AssistantActionRetryClassification.Retryable => PreparedActionRetryClassification.Retryable,
        AssistantActionRetryClassification.Idempotent => PreparedActionRetryClassification.Idempotent,
        _ => throw new ArgumentOutOfRangeException(nameof(classification), classification, null)
    };
}
