using MediatR;

namespace Vitarish.Clinic.Application.PatientAgent.Actions;

public enum PatientAgentActionOutcome
{
    Succeeded,
    NotFound,
    ScopeMismatch,
    PermissionDenied,
    Expired,
    Conflict,
    Failed
}

public sealed record PatientAgentActionResult(PatientAgentActionOutcome Outcome, PatientAgentAction? Action = null);

public sealed record ConfirmPatientAgentAction(Guid ActionId, PatientAgentContext Context)
    : IRequest<PatientAgentActionResult>;

public sealed class ConfirmPatientAgentActionHandler(
    IPatientAgentActionStore store,
    IPatientAgentActionCommitterRegistry committers,
    TimeProvider timeProvider) : IRequestHandler<ConfirmPatientAgentAction, PatientAgentActionResult>
{
    public async Task<PatientAgentActionResult> Handle(ConfirmPatientAgentAction request, CancellationToken cancellationToken)
    {
        PatientAgentAction? action = await store.GetAsync(request.ActionId, cancellationToken);
        PatientAgentActionOutcome? scopeFailure = ScopeFailure(action, request.Context);
        if (scopeFailure.HasValue) return new(scopeFailure.Value);
        if (action is null) return new(PatientAgentActionOutcome.NotFound);

        if (action.Status == PatientAgentActionStatus.Completed) return new(PatientAgentActionOutcome.Succeeded, action);
        if (action.Status is PatientAgentActionStatus.Rejected or PatientAgentActionStatus.Expired ||
            action.Status == PatientAgentActionStatus.Failed && action.Failure?.Retryable != true)
            return new(PatientAgentActionOutcome.Conflict, action);
        if (action.ExpiresAt <= timeProvider.GetUtcNow())
        {
            _ = await store.ExpireAsync(action.ActionId, cancellationToken);
            return new(PatientAgentActionOutcome.Expired, await store.GetAsync(action.ActionId, cancellationToken));
        }

        IPatientAgentActionCommitter? committer = committers.Resolve(action.ActionType);
        if (committer is null)
            return await SaveFailure(action, new("unsupported_action", "The action type cannot be confirmed.", false), cancellationToken);

        bool retryFailed = action.Status == PatientAgentActionStatus.Failed && action.Failure?.Retryable == true;
        if (!await store.TryBeginConfirmationAsync(action.ActionId, retryFailed, cancellationToken))
        {
            PatientAgentAction? latest = await store.GetAsync(action.ActionId, cancellationToken);
            return latest?.Status == PatientAgentActionStatus.Completed
                ? new(PatientAgentActionOutcome.Succeeded, latest)
                : new(PatientAgentActionOutcome.Conflict, latest);
        }

        PatientAgentActionCommitResult result;
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
            result = PatientAgentActionCommitResult.Failed(
                "unknown_commit_outcome", "The owning workflow returned an unknown outcome.");
        }

        if (!result.Succeeded)
            return await SaveFailure(action, result.Failure ?? new("commit_failed", "The action could not be completed.", false), cancellationToken);

        bool completed = await store.CompleteAsync(
            action.ActionId, timeProvider.GetUtcNow(), request.Context.AuthenticatedUserId,
            result.CompletionPayload ?? "{}", cancellationToken);
        PatientAgentAction? final = await store.GetAsync(action.ActionId, cancellationToken);
        return completed || final?.Status == PatientAgentActionStatus.Completed
            ? new(PatientAgentActionOutcome.Succeeded, final)
            : new(PatientAgentActionOutcome.Conflict, final);
    }

    private async Task<PatientAgentActionResult> SaveFailure(
        PatientAgentAction action,
        PatientAgentActionFailure failure,
        CancellationToken cancellationToken)
    {
        _ = await store.FailAsync(action.ActionId, failure, cancellationToken);
        return new(PatientAgentActionOutcome.Failed, await store.GetAsync(action.ActionId, cancellationToken));
    }

    internal static PatientAgentActionOutcome? ScopeFailure(PatientAgentAction? action, PatientAgentContext context)
    {
        if (action is null) return PatientAgentActionOutcome.NotFound;
        if (action.PatientId != context.PatientId || !string.Equals(action.PracticeId, context.PracticeId, StringComparison.Ordinal))
            return PatientAgentActionOutcome.ScopeMismatch;
        if (!string.Equals(action.RequestedByUserId, context.AuthenticatedUserId, StringComparison.Ordinal) ||
            !HasPermission(action.ActionType, context.Permissions))
            return PatientAgentActionOutcome.PermissionDenied;
        return null;
    }

    private static bool HasPermission(string actionType, IReadOnlySet<string> permissions) => actionType switch
    {
        "create_appointment" => permissions.Contains("Appointments.Create"),
        "create_prescription" => permissions.Contains("Prescriptions.Create"),
        "add_clinical_event" => permissions.Contains("ClinicalEvents.Create"),
        _ => false
    };
}

public sealed record RejectPatientAgentAction(Guid ActionId, PatientAgentContext Context, string? Reason)
    : IRequest<PatientAgentActionResult>;

public sealed class RejectPatientAgentActionHandler(IPatientAgentActionStore store, TimeProvider timeProvider)
    : IRequestHandler<RejectPatientAgentAction, PatientAgentActionResult>
{
    public async Task<PatientAgentActionResult> Handle(RejectPatientAgentAction request, CancellationToken cancellationToken)
    {
        PatientAgentAction? action = await store.GetAsync(request.ActionId, cancellationToken);
        PatientAgentActionOutcome? failure = ConfirmPatientAgentActionHandler.ScopeFailure(action, request.Context);
        if (failure.HasValue) return new(failure.Value);
        if (action!.Status != PatientAgentActionStatus.Proposed)
            return new(PatientAgentActionOutcome.Conflict, action);
        bool rejected = await store.TryRejectAsync(action.ActionId, timeProvider.GetUtcNow(), request.Reason, cancellationToken);
        return rejected
            ? new(PatientAgentActionOutcome.Succeeded, await store.GetAsync(action.ActionId, cancellationToken))
            : new(PatientAgentActionOutcome.Conflict, await store.GetAsync(action.ActionId, cancellationToken));
    }
}

public sealed record GetPatientAgentAction(Guid ActionId, PatientAgentContext Context)
    : IRequest<PatientAgentActionResult>;

public sealed class GetPatientAgentActionHandler(IPatientAgentActionStore store)
    : IRequestHandler<GetPatientAgentAction, PatientAgentActionResult>
{
    public async Task<PatientAgentActionResult> Handle(GetPatientAgentAction request, CancellationToken cancellationToken)
    {
        PatientAgentAction? action = await store.GetAsync(request.ActionId, cancellationToken);
        PatientAgentActionOutcome? failure = ConfirmPatientAgentActionHandler.ScopeFailure(action, request.Context);
        return failure.HasValue ? new(failure.Value) : new(PatientAgentActionOutcome.Succeeded, action);
    }
}
