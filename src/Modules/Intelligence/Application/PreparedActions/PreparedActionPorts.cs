using Vitarish.Intelligence.Domain.PreparedActions;

namespace Vitarish.Intelligence.Application.PreparedActions;

/// <summary>A stored prepared action and the opaque version its store needs to replace it safely.</summary>
public sealed record StoredPreparedAction(PreparedAction Action, string Version);

/// <summary>Keeps prepared actions. Every replacement is a compare-and-swap against the version that was read.</summary>
public interface IPreparedActionStore
{
    /// <summary>Saves a new action. Saving the identical action again is a no-op; a different action under the same ID throws.</summary>
    Task SaveNewAsync(PreparedAction action, CancellationToken cancellationToken);

    Task<StoredPreparedAction?> GetAsync(Guid actionId, CancellationToken cancellationToken);

    /// <summary>Replaces the action only if it is still at <paramref name="current"/>'s version.</summary>
    Task<bool> TryReplaceAsync(StoredPreparedAction current, PreparedAction next, CancellationToken cancellationToken);
}

/// <summary>Who is acting on prepared actions, as the calling app established it from the authenticated request.</summary>
public sealed record PreparedActionCaller(
    string AgentKey,
    string ScopeId,
    string SubjectKind,
    string SubjectId,
    string UserId);

/// <summary>Whether a failed commit of an action type may be confirmed again.</summary>
public enum PreparedActionRetryClassification
{
    NeverRetry,
    Retryable,
    Idempotent
}

/// <summary>The part of an app's action policy the lifecycle needs.</summary>
public sealed record PreparedActionPolicy(int Revision, PreparedActionRetryClassification RetryClassification);

/// <summary>What the owning workflow reported after trying to carry an action out.</summary>
public sealed record PreparedActionCommitResult(bool Succeeded, string? CompletionPayload, PreparedActionFailure? Failure)
{
    public static PreparedActionCommitResult Completed(string completionPayload) => new(true, completionPayload, null);

    public static PreparedActionCommitResult Failed(string code, string message, bool retryable = false) =>
        new(false, null, new(code, message, retryable));
}

/// <summary>
/// The calling app's side of one request against the lifecycle: who is calling, the app's policy and
/// authorization for an action type, and the owning workflow that carries an action out. The app builds it
/// from the authenticated request, so the lifecycle never sees app-specific request context.
/// </summary>
public interface IPreparedActionApp
{
    PreparedActionCaller Caller { get; }

    PreparedActionPolicy? ResolvePolicy(string actionType);

    /// <summary>Whether the caller's role and permissions allow acting on this action type now.</summary>
    bool Authorizes(string actionType);

    bool CanCommit(string actionType);

    /// <summary>Carries the action out; the owning workflow authorizes again within the caller's request.</summary>
    Task<PreparedActionCommitResult> CommitAsync(PreparedAction action, CancellationToken cancellationToken);
}
