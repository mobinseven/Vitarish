using MediatR;
using Microsoft.Extensions.Logging;
using Vitarish.Clinic.Application.PatientAgent.Operations;

namespace Vitarish.Clinic.Application.PatientAgent.Actions;

public enum PatientAgentActionOutcome
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

public sealed record PatientAgentActionResult(PatientAgentActionOutcome Outcome, PatientAgentAction? Action = null);

public sealed record ConfirmPatientAgentAction(Guid ActionId, PatientAgentContext Context)
    : IRequest<PatientAgentActionResult>;

public sealed class ConfirmPatientAgentActionHandler(
    IPatientAgentActionStore store,
    IPatientAgentActionCommitterRegistry committers,
    TimeProvider timeProvider,
    ILogger<ConfirmPatientAgentActionHandler>? logger = null) : IRequestHandler<ConfirmPatientAgentAction, PatientAgentActionResult>
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
        PatientAgentActionResult outcome = completed || final?.Status == PatientAgentActionStatus.Completed
            ? new(PatientAgentActionOutcome.Succeeded, final)
            : new(PatientAgentActionOutcome.Conflict, final);
        if (final is not null && logger is not null)
        {
            PatientAgentTelemetry.ActionCompleted(logger, final.ActionType, outcome.Outcome.ToString(), final.AgentRunId, final.ActionId);
        }
        return outcome;
    }

    private async Task<PatientAgentActionResult> SaveFailure(
        PatientAgentAction action,
        PatientAgentActionFailure failure,
        CancellationToken cancellationToken)
    {
        _ = await store.FailAsync(action.ActionId, failure, cancellationToken);
        PatientAgentAction? latest = await store.GetAsync(action.ActionId, cancellationToken);
        if (latest is not null && logger is not null)
        {
            PatientAgentTelemetry.ActionCompleted(logger, latest.ActionType, PatientAgentActionOutcome.Failed.ToString(), latest.AgentRunId, latest.ActionId);
        }
        return new(PatientAgentActionOutcome.Failed, latest);
    }

    internal static PatientAgentActionOutcome? ScopeFailure(PatientAgentAction? action, PatientAgentContext context)
    {
        if (action is null) return PatientAgentActionOutcome.NotFound;
        if (!string.Equals(action.SubjectKind, context.SubjectKind, StringComparison.Ordinal) ||
            action.PatientId != context.PatientId || !string.Equals(action.PracticeId, context.PracticeId, StringComparison.Ordinal))
            return PatientAgentActionOutcome.ScopeMismatch;
        if (!string.Equals(action.RequestedByUserId, context.AuthenticatedUserId, StringComparison.Ordinal) ||
            !HasPermission(action, context))
            return PatientAgentActionOutcome.PermissionDenied;
        return null;
    }

    private static bool HasPermission(PatientAgentAction action, PatientAgentContext context)
    {
        if (action.SubjectKind == "practice")
            return PatientAgentOperationsScope.IsAuthorizedRole(context.Role);
        return action.ActionType switch
        {
            "create_appointment" => context.Permissions.Contains("Appointments.Create"),
            "create_prescription" => context.Permissions.Contains("Prescriptions.Create"),
            "add_clinical_event" => context.Permissions.Contains("ClinicalEvents.Create"),
            _ => false
        };
    }
}

public sealed record RejectPatientAgentAction(Guid ActionId, PatientAgentContext Context, string? Reason)
    : IRequest<PatientAgentActionResult>;

public sealed class RejectPatientAgentActionHandler(IPatientAgentActionStore store, TimeProvider timeProvider)
    : IRequestHandler<RejectPatientAgentAction, PatientAgentActionResult>
{
    public async Task<PatientAgentActionResult> Handle(RejectPatientAgentAction request, CancellationToken cancellationToken)
    {
        if (request.Reason?.Length > 1_000)
            return new(PatientAgentActionOutcome.ValidationFailure);
        PatientAgentAction? action = await store.GetAsync(request.ActionId, cancellationToken);
        PatientAgentActionOutcome? failure = ConfirmPatientAgentActionHandler.ScopeFailure(action, request.Context);
        if (failure.HasValue) return new(failure.Value);
        if (action!.ExpiresAt <= timeProvider.GetUtcNow())
        {
            _ = await store.ExpireAsync(action.ActionId, cancellationToken);
            return new(PatientAgentActionOutcome.Expired, await store.GetAsync(action.ActionId, cancellationToken));
        }
        if (action!.Status != PatientAgentActionStatus.Proposed)
            return new(PatientAgentActionOutcome.Conflict, action);
        string? reason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim();
        bool rejected = await store.TryRejectAsync(action.ActionId, timeProvider.GetUtcNow(), reason, cancellationToken);
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
