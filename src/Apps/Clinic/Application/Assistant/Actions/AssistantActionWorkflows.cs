using MediatR;
using Microsoft.Extensions.Logging;
using Vitarish.Clinic.Application.Assistant.Capabilities;
using Vitarish.Clinic.Application.Assistant.Operations;

namespace Vitarish.Clinic.Application.Assistant.Actions;

public enum AssistantActionOutcome
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

public sealed record AssistantActionResult(AssistantActionOutcome Outcome, AssistantAction? Action = null);

public sealed record ConfirmAssistantAction(Guid ActionId, AssistantContext Context)
    : IRequest<AssistantActionResult>;

public sealed class ConfirmAssistantActionHandler(
    IAssistantActionStore store,
    IAssistantActionCommitterRegistry committers,
    AssistantCapabilityRegistry capabilityRegistry,
    TimeProvider timeProvider,
    ILogger<ConfirmAssistantActionHandler>? logger = null) : IRequestHandler<ConfirmAssistantAction, AssistantActionResult>
{
    public async Task<AssistantActionResult> Handle(ConfirmAssistantAction request, CancellationToken cancellationToken)
    {
        AssistantAction? action = await store.GetAsync(request.ActionId, cancellationToken);
        AssistantActionOutcome? scopeFailure = ScopeFailure(action, request.Context, capabilityRegistry);
        if (scopeFailure.HasValue) return new(scopeFailure.Value);
        if (action is null) return new(AssistantActionOutcome.NotFound);

        if (action.Status == AssistantActionStatus.Completed) return new(AssistantActionOutcome.Succeeded, action);
        if (action.Status is AssistantActionStatus.Rejected or AssistantActionStatus.Expired ||
            action.Status == AssistantActionStatus.Failed && action.Failure?.Retryable != true)
            return new(AssistantActionOutcome.Conflict, action);
        if (action.ExpiresAt <= timeProvider.GetUtcNow())
        {
            _ = await store.ExpireAsync(action.ActionId, cancellationToken);
            return new(AssistantActionOutcome.Expired, await store.GetAsync(action.ActionId, cancellationToken));
        }

        AssistantActionPolicy? policy = capabilityRegistry.ResolveAction(action.ActionType);
        if (policy is null)
            return await SaveFailure(action, new("undeclared_action", "The action policy is no longer available.", false), cancellationToken);
        if (action.ActionPolicyRevision > 0 && action.ActionPolicyRevision != policy.Revision)
            return await SaveFailure(action, new("stale_action_policy", "The action policy changed; the action must be proposed again.", false), cancellationToken);
        if (!policy.Authorizes(request.Context))
            return new(AssistantActionOutcome.PermissionDenied, action);

        IAssistantActionCommitter? committer = committers.Resolve(action.ActionType);
        if (committer is null)
            return await SaveFailure(action, new("unsupported_action", "The action type cannot be confirmed.", false), cancellationToken);

        bool retryFailed = action.Status == AssistantActionStatus.Failed &&
            action.Failure?.Retryable == true &&
            policy.RetryClassification is AssistantActionRetryClassification.Retryable or AssistantActionRetryClassification.Idempotent;
        if (retryFailed && logger is not null)
            AssistantTelemetry.ActionRetry(logger, action.ActionType, policy.RetryClassification.ToString(), action.ActionId);
        if (!await store.TryBeginConfirmationAsync(action.ActionId, retryFailed, cancellationToken))
        {
            AssistantAction? latest = await store.GetAsync(action.ActionId, cancellationToken);
            if (logger is not null)
                AssistantTelemetry.ActionConfirmationClaimed(logger, action.ActionType, "conflict", action.ActionId);
            return latest?.Status == AssistantActionStatus.Completed
                ? new(AssistantActionOutcome.Succeeded, latest)
                : new(AssistantActionOutcome.Conflict, latest);
        }

        AssistantActionCommitResult result;
        try
        {
            result = await committer.CommitAsync(action, request.Context, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            // The owning mutation may have succeeded; an automatic retry would be unsafe.
            result = AssistantActionCommitResult.Failed(
                "unknown_commit_outcome", "The owning workflow returned an unknown outcome.");
        }

        if (!result.Succeeded)
        {
            AssistantActionFailure failure = result.Failure ?? new("commit_failed", "The action could not be completed.", false);
            if (policy.RetryClassification == AssistantActionRetryClassification.NeverRetry)
                failure = failure with { Retryable = false };
            if (logger is not null)
                AssistantTelemetry.ActionFailed(logger, action.ActionType, action.ActionId, failure.Retryable);
            return await SaveFailure(action, failure, cancellationToken);
        }

        if (logger is not null)
            AssistantTelemetry.ActionConfirmationClaimed(logger, action.ActionType, "claimed", action.ActionId);

        bool completed = await store.CompleteAsync(
            action.ActionId, timeProvider.GetUtcNow(), request.Context.AuthenticatedUserId,
            result.CompletionPayload ?? "{}", cancellationToken);
        AssistantAction? final = await store.GetAsync(action.ActionId, cancellationToken);
        AssistantActionResult outcome = completed || final?.Status == AssistantActionStatus.Completed
            ? new(AssistantActionOutcome.Succeeded, final)
            : new(AssistantActionOutcome.Conflict, final);
        if (final is not null && logger is not null)
        {
            AssistantTelemetry.ActionCompleted(logger, final.ActionType, outcome.Outcome.ToString(), final.AgentRunId, final.ActionId);
        }
        return outcome;
    }

    private async Task<AssistantActionResult> SaveFailure(
        AssistantAction action,
        AssistantActionFailure failure,
        CancellationToken cancellationToken)
    {
        _ = await store.FailAsync(action.ActionId, failure, cancellationToken);
        AssistantAction? latest = await store.GetAsync(action.ActionId, cancellationToken);
        if (latest is not null && logger is not null)
        {
            AssistantTelemetry.ActionCompleted(logger, latest.ActionType, AssistantActionOutcome.Failed.ToString(), latest.AgentRunId, latest.ActionId);
        }
        return new(AssistantActionOutcome.Failed, latest);
    }

    internal static AssistantActionOutcome? ScopeFailure(
        AssistantAction? action,
        AssistantContext context,
        AssistantCapabilityRegistry capabilityRegistry)
    {
        if (action is null) return AssistantActionOutcome.NotFound;
        if (!string.Equals(action.SubjectKind, context.SubjectKind, StringComparison.Ordinal) ||
            action.PatientId != context.PatientId || !string.Equals(action.PracticeId, context.PracticeId, StringComparison.Ordinal))
            return AssistantActionOutcome.ScopeMismatch;
        AssistantActionPolicy? policy = capabilityRegistry.ResolveAction(action.ActionType);
        if (!string.Equals(action.RequestedByUserId, context.AuthenticatedUserId, StringComparison.Ordinal) ||
            policy is null ||
            !policy.Authorizes(context))
            return AssistantActionOutcome.PermissionDenied;
        return null;
    }
}

public sealed record RejectAssistantAction(Guid ActionId, AssistantContext Context, string? Reason)
    : IRequest<AssistantActionResult>;

public sealed class RejectAssistantActionHandler(IAssistantActionStore store, AssistantCapabilityRegistry capabilityRegistry, TimeProvider timeProvider)
    : IRequestHandler<RejectAssistantAction, AssistantActionResult>
{
    public async Task<AssistantActionResult> Handle(RejectAssistantAction request, CancellationToken cancellationToken)
    {
        if (request.Reason?.Length > 1_000)
            return new(AssistantActionOutcome.ValidationFailure);
        AssistantAction? action = await store.GetAsync(request.ActionId, cancellationToken);
        AssistantActionOutcome? failure = ConfirmAssistantActionHandler.ScopeFailure(action, request.Context, capabilityRegistry);
        if (failure.HasValue) return new(failure.Value);
        if (action!.ExpiresAt <= timeProvider.GetUtcNow())
        {
            _ = await store.ExpireAsync(action.ActionId, cancellationToken);
            return new(AssistantActionOutcome.Expired, await store.GetAsync(action.ActionId, cancellationToken));
        }
        if (action!.Status != AssistantActionStatus.Proposed)
            return new(AssistantActionOutcome.Conflict, action);
        string? reason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim();
        bool rejected = await store.TryRejectAsync(action.ActionId, timeProvider.GetUtcNow(), reason, cancellationToken);
        return rejected
            ? new(AssistantActionOutcome.Succeeded, await store.GetAsync(action.ActionId, cancellationToken))
            : new(AssistantActionOutcome.Conflict, await store.GetAsync(action.ActionId, cancellationToken));
    }
}

public sealed record GetAssistantAction(Guid ActionId, AssistantContext Context)
    : IRequest<AssistantActionResult>;

public sealed class GetAssistantActionHandler(IAssistantActionStore store, AssistantCapabilityRegistry capabilityRegistry)
    : IRequestHandler<GetAssistantAction, AssistantActionResult>
{
    public async Task<AssistantActionResult> Handle(GetAssistantAction request, CancellationToken cancellationToken)
    {
        AssistantAction? action = await store.GetAsync(request.ActionId, cancellationToken);
        AssistantActionOutcome? failure = ConfirmAssistantActionHandler.ScopeFailure(action, request.Context, capabilityRegistry);
        return failure.HasValue ? new(failure.Value) : new(AssistantActionOutcome.Succeeded, action);
    }
}
