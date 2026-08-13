using MediatR;
using Microsoft.Extensions.Logging;
using Vitarish.Clinic.Application.ClinicWorkspace.Operations;

namespace Vitarish.Clinic.Application.ClinicWorkspace.Actions;

public enum ClinicWorkspaceActionOutcome
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

public sealed record ClinicWorkspaceActionResult(ClinicWorkspaceActionOutcome Outcome, ClinicWorkspaceAction? Action = null);

public sealed record ConfirmClinicWorkspaceAction(Guid ActionId, ClinicWorkspaceContext Context)
    : IRequest<ClinicWorkspaceActionResult>;

public sealed class ConfirmClinicWorkspaceActionHandler(
    IClinicWorkspaceActionStore store,
    IClinicWorkspaceActionCommitterRegistry committers,
    TimeProvider timeProvider,
    ILogger<ConfirmClinicWorkspaceActionHandler>? logger = null) : IRequestHandler<ConfirmClinicWorkspaceAction, ClinicWorkspaceActionResult>
{
    public async Task<ClinicWorkspaceActionResult> Handle(ConfirmClinicWorkspaceAction request, CancellationToken cancellationToken)
    {
        ClinicWorkspaceAction? action = await store.GetAsync(request.ActionId, cancellationToken);
        ClinicWorkspaceActionOutcome? scopeFailure = ScopeFailure(action, request.Context);
        if (scopeFailure.HasValue) return new(scopeFailure.Value);
        if (action is null) return new(ClinicWorkspaceActionOutcome.NotFound);

        if (action.Status == ClinicWorkspaceActionStatus.Completed) return new(ClinicWorkspaceActionOutcome.Succeeded, action);
        if (action.Status is ClinicWorkspaceActionStatus.Rejected or ClinicWorkspaceActionStatus.Expired ||
            action.Status == ClinicWorkspaceActionStatus.Failed && action.Failure?.Retryable != true)
            return new(ClinicWorkspaceActionOutcome.Conflict, action);
        if (action.ExpiresAt <= timeProvider.GetUtcNow())
        {
            _ = await store.ExpireAsync(action.ActionId, cancellationToken);
            return new(ClinicWorkspaceActionOutcome.Expired, await store.GetAsync(action.ActionId, cancellationToken));
        }

        IClinicWorkspaceActionCommitter? committer = committers.Resolve(action.ActionType);
        if (committer is null)
            return await SaveFailure(action, new("unsupported_action", "The action type cannot be confirmed.", false), cancellationToken);

        bool retryFailed = action.Status == ClinicWorkspaceActionStatus.Failed && action.Failure?.Retryable == true;
        if (!await store.TryBeginConfirmationAsync(action.ActionId, retryFailed, cancellationToken))
        {
            ClinicWorkspaceAction? latest = await store.GetAsync(action.ActionId, cancellationToken);
            return latest?.Status == ClinicWorkspaceActionStatus.Completed
                ? new(ClinicWorkspaceActionOutcome.Succeeded, latest)
                : new(ClinicWorkspaceActionOutcome.Conflict, latest);
        }

        ClinicWorkspaceActionCommitResult result;
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
            result = ClinicWorkspaceActionCommitResult.Failed(
                "unknown_commit_outcome", "The owning workflow returned an unknown outcome.");
        }

        if (!result.Succeeded)
            return await SaveFailure(action, result.Failure ?? new("commit_failed", "The action could not be completed.", false), cancellationToken);

        bool completed = await store.CompleteAsync(
            action.ActionId, timeProvider.GetUtcNow(), request.Context.AuthenticatedUserId,
            result.CompletionPayload ?? "{}", cancellationToken);
        ClinicWorkspaceAction? final = await store.GetAsync(action.ActionId, cancellationToken);
        ClinicWorkspaceActionResult outcome = completed || final?.Status == ClinicWorkspaceActionStatus.Completed
            ? new(ClinicWorkspaceActionOutcome.Succeeded, final)
            : new(ClinicWorkspaceActionOutcome.Conflict, final);
        if (final is not null && logger is not null)
        {
            ClinicWorkspaceTelemetry.ActionCompleted(logger, final.ActionType, outcome.Outcome.ToString(), final.AgentRunId, final.ActionId);
        }
        return outcome;
    }

    private async Task<ClinicWorkspaceActionResult> SaveFailure(
        ClinicWorkspaceAction action,
        ClinicWorkspaceActionFailure failure,
        CancellationToken cancellationToken)
    {
        _ = await store.FailAsync(action.ActionId, failure, cancellationToken);
        ClinicWorkspaceAction? latest = await store.GetAsync(action.ActionId, cancellationToken);
        if (latest is not null && logger is not null)
        {
            ClinicWorkspaceTelemetry.ActionCompleted(logger, latest.ActionType, ClinicWorkspaceActionOutcome.Failed.ToString(), latest.AgentRunId, latest.ActionId);
        }
        return new(ClinicWorkspaceActionOutcome.Failed, latest);
    }

    internal static ClinicWorkspaceActionOutcome? ScopeFailure(ClinicWorkspaceAction? action, ClinicWorkspaceContext context)
    {
        if (action is null) return ClinicWorkspaceActionOutcome.NotFound;
        if (!string.Equals(action.SubjectKind, context.SubjectKind, StringComparison.Ordinal) ||
            action.PatientId != context.PatientId || !string.Equals(action.PracticeId, context.PracticeId, StringComparison.Ordinal))
            return ClinicWorkspaceActionOutcome.ScopeMismatch;
        if (!string.Equals(action.RequestedByUserId, context.AuthenticatedUserId, StringComparison.Ordinal) ||
            !HasPermission(action, context))
            return ClinicWorkspaceActionOutcome.PermissionDenied;
        return null;
    }

    private static bool HasPermission(ClinicWorkspaceAction action, ClinicWorkspaceContext context)
    {
        if (action.SubjectKind == "practice")
            return ClinicWorkspaceOperationsScope.IsAuthorizedRole(context.Role);
        return action.ActionType switch
        {
            "create_appointment" => context.Permissions.Contains("Appointments.Create"),
            "create_prescription" => context.Permissions.Contains("Prescriptions.Create"),
            "add_clinical_event" => context.Permissions.Contains("ClinicalEvents.Create"),
            _ => false
        };
    }
}

public sealed record RejectClinicWorkspaceAction(Guid ActionId, ClinicWorkspaceContext Context, string? Reason)
    : IRequest<ClinicWorkspaceActionResult>;

public sealed class RejectClinicWorkspaceActionHandler(IClinicWorkspaceActionStore store, TimeProvider timeProvider)
    : IRequestHandler<RejectClinicWorkspaceAction, ClinicWorkspaceActionResult>
{
    public async Task<ClinicWorkspaceActionResult> Handle(RejectClinicWorkspaceAction request, CancellationToken cancellationToken)
    {
        if (request.Reason?.Length > 1_000)
            return new(ClinicWorkspaceActionOutcome.ValidationFailure);
        ClinicWorkspaceAction? action = await store.GetAsync(request.ActionId, cancellationToken);
        ClinicWorkspaceActionOutcome? failure = ConfirmClinicWorkspaceActionHandler.ScopeFailure(action, request.Context);
        if (failure.HasValue) return new(failure.Value);
        if (action!.ExpiresAt <= timeProvider.GetUtcNow())
        {
            _ = await store.ExpireAsync(action.ActionId, cancellationToken);
            return new(ClinicWorkspaceActionOutcome.Expired, await store.GetAsync(action.ActionId, cancellationToken));
        }
        if (action!.Status != ClinicWorkspaceActionStatus.Proposed)
            return new(ClinicWorkspaceActionOutcome.Conflict, action);
        string? reason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim();
        bool rejected = await store.TryRejectAsync(action.ActionId, timeProvider.GetUtcNow(), reason, cancellationToken);
        return rejected
            ? new(ClinicWorkspaceActionOutcome.Succeeded, await store.GetAsync(action.ActionId, cancellationToken))
            : new(ClinicWorkspaceActionOutcome.Conflict, await store.GetAsync(action.ActionId, cancellationToken));
    }
}

public sealed record GetClinicWorkspaceAction(Guid ActionId, ClinicWorkspaceContext Context)
    : IRequest<ClinicWorkspaceActionResult>;

public sealed class GetClinicWorkspaceActionHandler(IClinicWorkspaceActionStore store)
    : IRequestHandler<GetClinicWorkspaceAction, ClinicWorkspaceActionResult>
{
    public async Task<ClinicWorkspaceActionResult> Handle(GetClinicWorkspaceAction request, CancellationToken cancellationToken)
    {
        ClinicWorkspaceAction? action = await store.GetAsync(request.ActionId, cancellationToken);
        ClinicWorkspaceActionOutcome? failure = ConfirmClinicWorkspaceActionHandler.ScopeFailure(action, request.Context);
        return failure.HasValue ? new(failure.Value) : new(ClinicWorkspaceActionOutcome.Succeeded, action);
    }
}
