using Microsoft.Extensions.Logging;
using Vitarish.Intelligence.Domain.PreparedActions;

namespace Vitarish.Intelligence.Application.PreparedActions;

public enum PreparedActionOutcome
{
    Succeeded,
    ValidationFailure,
    NotFound,
    ScopeMismatch,
    PermissionDenied,
    Expired,
    Conflict,
    Failed
}

public sealed record PreparedActionResult(PreparedActionOutcome Outcome, PreparedAction? Action = null);

/// <summary>Why the lifecycle itself rejected a proposal.</summary>
public static class PreparedActionRejectionReasons
{
    /// <summary>The person edited the proposal; a replacement in the same run took its place.</summary>
    public const string Revised = "revised";
}

/// <summary>Failure codes the lifecycle itself records; owning workflows report their own codes.</summary>
public static class PreparedActionFailureCodes
{
    public const string UndeclaredAction = "undeclared_action";
    public const string StaleActionPolicy = "stale_action_policy";
    public const string UnsupportedAction = "unsupported_action";
    public const string UnknownCommitOutcome = "unknown_commit_outcome";
    public const string CommitFailed = "commit_failed";
}

/// <summary>
/// Owns how a prepared action lives: it is prepared, read, confirmed (with retry of retryable failures) or
/// rejected, and it expires. Every call is made by an app on behalf of an authenticated request and checks,
/// in order, that the action exists, belongs to the caller's agent, scope and subject, was requested by the
/// caller, and is allowed by the app's policy, before anything changes.
/// </summary>
public sealed class PreparedActionLifecycle(
    IPreparedActionStore store,
    TimeProvider timeProvider,
    ILogger<PreparedActionLifecycle>? logger = null)
{
    public const int MaxRejectionReasonLength = 1_000;

    // Compare-and-swap attempts per transition. A failed swap means another request changed the action
    // meanwhile; the transition is evaluated again on the newer state, as an atomic store script would.
    private const int MaxTransitionAttempts = 5;

    public async Task PrepareAsync(PreparedAction action, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (action.Status != PreparedActionStatus.Proposed)
            throw new ArgumentException("A prepared action starts as proposed.", nameof(action));
        await store.SaveNewAsync(action, cancellationToken);
        if (logger is not null)
            PreparedActionTelemetry.Prepared(logger, action.ActionType, action.AgentKey, action.AgentRunId, action.ActionId);
    }

    public async Task<PreparedActionResult> GetAsync(Guid actionId, IPreparedActionApp app, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(app);
        StoredPreparedAction? stored = await store.GetAsync(actionId, cancellationToken);
        PreparedActionOutcome? failure = AccessFailure(stored?.Action, app);
        return failure.HasValue ? new(failure.Value) : new(PreparedActionOutcome.Succeeded, stored!.Action);
    }

    public async Task<PreparedActionResult> RejectAsync(
        Guid actionId,
        IPreparedActionApp app,
        string? reason,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(app);
        if (reason?.Length > MaxRejectionReasonLength)
            return new(PreparedActionOutcome.ValidationFailure);
        StoredPreparedAction? stored = await store.GetAsync(actionId, cancellationToken);
        PreparedActionOutcome? failure = AccessFailure(stored?.Action, app);
        if (failure.HasValue) return new(failure.Value);
        PreparedAction action = stored!.Action;

        if (action.IsExpiredAt(timeProvider.GetUtcNow()))
            return new(PreparedActionOutcome.Expired, await ExpireAsync(actionId, cancellationToken));
        if (action.Status != PreparedActionStatus.Proposed)
            return new(PreparedActionOutcome.Conflict, action);

        string? normalized = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        DateTimeOffset rejectedAt = timeProvider.GetUtcNow();
        bool rejected = await TransitionAsync(actionId, current => current.Reject(rejectedAt, normalized), cancellationToken);
        PreparedAction? latest = await ReadAsync(actionId, cancellationToken);
        return new(rejected ? PreparedActionOutcome.Succeeded : PreparedActionOutcome.Conflict, latest);
    }

    /// <summary>
    /// Replaces a proposal with an edited one in the same run. The replacement is prepared first, so an invalid edit
    /// changes nothing; then the original is rejected as revised, and only then the replacement is saved. A failure
    /// between those two writes leaves no live proposal, never two.
    /// </summary>
    public async Task<PreparedActionResult> ReviseAsync(
        Guid actionId,
        IPreparedActionApp app,
        Func<PreparedAction, CancellationToken, Task<PreparedAction?>> prepareReplacement,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(prepareReplacement);
        StoredPreparedAction? stored = await store.GetAsync(actionId, cancellationToken);
        PreparedActionOutcome? failure = AccessFailure(stored?.Action, app);
        if (failure.HasValue) return new(failure.Value);
        PreparedAction original = stored!.Action;

        if (original.IsExpiredAt(timeProvider.GetUtcNow()))
            return new(PreparedActionOutcome.Expired, await ExpireAsync(actionId, cancellationToken));
        if (original.Status != PreparedActionStatus.Proposed)
            return new(PreparedActionOutcome.Conflict, original);

        PreparedAction? replacement = await prepareReplacement(original, cancellationToken);
        if (replacement is null)
            return new(PreparedActionOutcome.ValidationFailure, original);
        if (!IsReplacementFor(replacement, original))
            throw new InvalidOperationException("A revision must stay a proposed action of the same type, run, owner and scope.");

        DateTimeOffset revisedAt = timeProvider.GetUtcNow();
        if (!await TransitionAsync(actionId, current => current.Reject(revisedAt, PreparedActionRejectionReasons.Revised), cancellationToken))
            return new(PreparedActionOutcome.Conflict, await ReadAsync(actionId, cancellationToken));
        await store.SaveNewAsync(replacement, cancellationToken);
        if (logger is not null)
            PreparedActionTelemetry.Prepared(logger, replacement.ActionType, replacement.AgentKey, replacement.AgentRunId, replacement.ActionId);
        return new(PreparedActionOutcome.Succeeded, replacement);
    }

    private static bool IsReplacementFor(PreparedAction replacement, PreparedAction original) =>
        replacement.Status == PreparedActionStatus.Proposed &&
        replacement.ActionId != original.ActionId &&
        string.Equals(replacement.AgentKey, original.AgentKey, StringComparison.Ordinal) &&
        string.Equals(replacement.ScopeId, original.ScopeId, StringComparison.Ordinal) &&
        string.Equals(replacement.SubjectKind, original.SubjectKind, StringComparison.Ordinal) &&
        string.Equals(replacement.SubjectId, original.SubjectId, StringComparison.Ordinal) &&
        string.Equals(replacement.RequestedByUserId, original.RequestedByUserId, StringComparison.Ordinal) &&
        string.Equals(replacement.AgentRunId, original.AgentRunId, StringComparison.Ordinal) &&
        string.Equals(replacement.ConversationId, original.ConversationId, StringComparison.Ordinal) &&
        string.Equals(replacement.Capability, original.Capability, StringComparison.Ordinal) &&
        string.Equals(replacement.ActionType, original.ActionType, StringComparison.Ordinal);

    public async Task<PreparedActionResult> ConfirmAsync(Guid actionId, IPreparedActionApp app, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(app);
        StoredPreparedAction? stored = await store.GetAsync(actionId, cancellationToken);
        PreparedActionOutcome? accessFailure = AccessFailure(stored?.Action, app);
        if (accessFailure.HasValue) return new(accessFailure.Value);
        PreparedAction action = stored!.Action;

        if (action.Status == PreparedActionStatus.Completed)
            return new(PreparedActionOutcome.Succeeded, action);
        if (action.Status is PreparedActionStatus.Rejected or PreparedActionStatus.Expired ||
            action.Status == PreparedActionStatus.Failed && action.Failure?.Retryable != true)
            return new(PreparedActionOutcome.Conflict, action);
        if (action.IsExpiredAt(timeProvider.GetUtcNow()))
            return new(PreparedActionOutcome.Expired, await ExpireAsync(actionId, cancellationToken));

        PreparedActionPolicy? policy = app.ResolvePolicy(action.ActionType);
        if (policy is null)
            return await FailAsync(action, new(PreparedActionFailureCodes.UndeclaredAction, "The action policy is no longer available.", false), cancellationToken);
        if (action.PolicyRevision != policy.Revision)
            return await FailAsync(action, new(PreparedActionFailureCodes.StaleActionPolicy, "The action policy changed; the action must be proposed again.", false), cancellationToken);
        if (!app.Authorizes(action.ActionType))
            return new(PreparedActionOutcome.PermissionDenied, action);
        if (!app.CanCommit(action.ActionType))
            return await FailAsync(action, new(PreparedActionFailureCodes.UnsupportedAction, "The action type cannot be confirmed.", false), cancellationToken);

        bool retryFailed = action.Status == PreparedActionStatus.Failed &&
            action.Failure?.Retryable == true &&
            policy.RetryClassification is PreparedActionRetryClassification.Retryable or PreparedActionRetryClassification.Idempotent;
        if (retryFailed && logger is not null)
            PreparedActionTelemetry.Retry(logger, action.ActionType, policy.RetryClassification.ToString(), action.ActionId);

        if (!await TransitionAsync(actionId, current => current.BeginConfirmation(retryFailed), cancellationToken))
        {
            PreparedAction? latest = await ReadAsync(actionId, cancellationToken);
            if (logger is not null)
                PreparedActionTelemetry.ConfirmationClaimed(logger, action.ActionType, "conflict", action.ActionId);
            return latest?.Status == PreparedActionStatus.Completed
                ? new(PreparedActionOutcome.Succeeded, latest)
                : new(PreparedActionOutcome.Conflict, latest);
        }
        if (logger is not null)
            PreparedActionTelemetry.ConfirmationClaimed(logger, action.ActionType, "claimed", action.ActionId);

        PreparedActionCommitResult result;
        try
        {
            result = await app.CommitAsync(action, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            // The owning mutation may have succeeded; an automatic retry would be unsafe.
            result = PreparedActionCommitResult.Failed(
                PreparedActionFailureCodes.UnknownCommitOutcome, "The owning workflow returned an unknown outcome.");
        }

        if (!result.Succeeded)
        {
            PreparedActionFailure failure = result.Failure ??
                new(PreparedActionFailureCodes.CommitFailed, "The action could not be completed.", false);
            if (policy.RetryClassification == PreparedActionRetryClassification.NeverRetry)
                failure = failure with { Retryable = false };
            if (logger is not null)
                PreparedActionTelemetry.Failed(logger, action.ActionType, action.ActionId, failure.Retryable);
            return await FailAsync(action, failure, cancellationToken);
        }

        DateTimeOffset confirmedAt = timeProvider.GetUtcNow();
        string completionPayload = result.CompletionPayload ?? "{}";
        bool completed = await TransitionAsync(
            actionId, current => current.Complete(confirmedAt, app.Caller.UserId, completionPayload), cancellationToken);
        PreparedAction? final = await ReadAsync(actionId, cancellationToken);
        PreparedActionResult outcome = completed || final?.Status == PreparedActionStatus.Completed
            ? new(PreparedActionOutcome.Succeeded, final)
            : new(PreparedActionOutcome.Conflict, final);
        if (final is not null && logger is not null)
            PreparedActionTelemetry.Completed(logger, final.ActionType, final.AgentKey, outcome.Outcome.ToString(), final.AgentRunId, final.ActionId);
        return outcome;
    }

    private static PreparedActionOutcome? AccessFailure(PreparedAction? action, IPreparedActionApp app)
    {
        if (action is null) return PreparedActionOutcome.NotFound;
        PreparedActionCaller caller = app.Caller;
        if (!string.Equals(action.AgentKey, caller.AgentKey, StringComparison.Ordinal) ||
            !string.Equals(action.ScopeId, caller.ScopeId, StringComparison.Ordinal) ||
            !string.Equals(action.SubjectKind, caller.SubjectKind, StringComparison.Ordinal) ||
            !string.Equals(action.SubjectId, caller.SubjectId, StringComparison.Ordinal))
            return PreparedActionOutcome.ScopeMismatch;
        if (!string.Equals(action.RequestedByUserId, caller.UserId, StringComparison.Ordinal) ||
            app.ResolvePolicy(action.ActionType) is null ||
            !app.Authorizes(action.ActionType))
            return PreparedActionOutcome.PermissionDenied;
        return null;
    }

    private async Task<PreparedActionResult> FailAsync(
        PreparedAction action,
        PreparedActionFailure failure,
        CancellationToken cancellationToken)
    {
        _ = await TransitionAsync(action.ActionId, current => current.Fail(failure), cancellationToken);
        PreparedAction? latest = await ReadAsync(action.ActionId, cancellationToken);
        if (latest is not null && logger is not null)
            PreparedActionTelemetry.Completed(logger, latest.ActionType, latest.AgentKey, PreparedActionOutcome.Failed.ToString(), latest.AgentRunId, latest.ActionId);
        return new(PreparedActionOutcome.Failed, latest);
    }

    private async Task<PreparedAction?> ExpireAsync(Guid actionId, CancellationToken cancellationToken)
    {
        _ = await TransitionAsync(actionId, current => current.Expire(), cancellationToken);
        return await ReadAsync(actionId, cancellationToken);
    }

    private async Task<PreparedAction?> ReadAsync(Guid actionId, CancellationToken cancellationToken) =>
        (await store.GetAsync(actionId, cancellationToken))?.Action;

    private async Task<bool> TransitionAsync(
        Guid actionId,
        Func<PreparedAction, PreparedAction?> transition,
        CancellationToken cancellationToken)
    {
        for (int attempt = 0; attempt < MaxTransitionAttempts; attempt++)
        {
            StoredPreparedAction? current = await store.GetAsync(actionId, cancellationToken);
            if (current is null) return false;
            PreparedAction? next = transition(current.Action);
            if (next is null) return false;
            if (await store.TryReplaceAsync(current, next, cancellationToken)) return true;
        }
        return false;
    }
}
