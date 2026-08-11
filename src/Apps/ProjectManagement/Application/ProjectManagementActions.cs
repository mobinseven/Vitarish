using MediatR;
using Vitarish.Intelligence.Application.Agents;
using Vitarish.Sea.Application.Agents;

namespace Vitarish.ProjectManagement.Application;

public enum ProjectManagementActionStatus { Proposed, Confirming, Completed, Rejected, Expired, Failed }
public sealed record ProjectManagementActionFailure(string Code, string Message, bool Retryable = false);
public sealed record ProjectManagementAction(Guid ActionId, string TenantId, string RequestedByUserId, Guid? EventId, string AgentRunId, string ToolCallId, SeaManagementPreparedAction PreparedAction, ProjectManagementActionStatus Status, DateTimeOffset CreatedAt, DateTimeOffset ExpiresAt, string? CompletionPayload = null, ProjectManagementActionFailure? Failure = null, string? RejectionReason = null, string ScopeKind = "event", string? SubjectId = null);
public interface IProjectManagementActionStore
{
    Task SaveAsync(ProjectManagementAction action, CancellationToken cancellationToken);
    Task<ProjectManagementAction?> GetAsync(Guid actionId, CancellationToken cancellationToken);
    Task<bool> TryBeginConfirmationAsync(Guid actionId, CancellationToken cancellationToken);
    Task<bool> TryRejectAsync(Guid actionId, string? reason, CancellationToken cancellationToken);
    Task<bool> CompleteAsync(Guid actionId, string payload, CancellationToken cancellationToken);
    Task<bool> FailAsync(Guid actionId, ProjectManagementActionFailure failure, CancellationToken cancellationToken);
    Task<bool> ExpireAsync(Guid actionId, CancellationToken cancellationToken);
}
public enum ProjectManagementActionOutcome { Succeeded, NotFound, Forbidden, Conflict, Expired, Failed, ValidationFailure }
public sealed record ProjectManagementActionResult(ProjectManagementActionOutcome Outcome, ProjectManagementAction? Action = null);

public sealed class ProjectManagementActionWorkflows(IProjectManagementActionStore store, ISeaManagementAgentTool seaTool, TimeProvider timeProvider)
{
    public async Task<ProjectManagementActionResult> ConfirmAsync(Guid actionId, ProjectManagementAgentContext context, CancellationToken cancellationToken)
    {
        ProjectManagementAction? action = await store.GetAsync(actionId, cancellationToken);
        if (action is null)
        {
            return new(ProjectManagementActionOutcome.NotFound);
        }

        if (!OwnedBy(action, context))
        {
            return new(ProjectManagementActionOutcome.Forbidden);
        }

        if (action.Status == ProjectManagementActionStatus.Completed)
        {
            return new(ProjectManagementActionOutcome.Succeeded, action);
        }

        if (action.ExpiresAt <= timeProvider.GetUtcNow()) { _ = await store.ExpireAsync(actionId, cancellationToken); return new(ProjectManagementActionOutcome.Expired, await store.GetAsync(actionId, cancellationToken)); }
        if (action.Status is ProjectManagementActionStatus.Rejected or ProjectManagementActionStatus.Expired or ProjectManagementActionStatus.Confirming || action.Status == ProjectManagementActionStatus.Failed && action.Failure?.Retryable != true)
        {
            return new(ProjectManagementActionOutcome.Conflict, action);
        }

        if (!await store.TryBeginConfirmationAsync(actionId, cancellationToken))
        {
            return new(ProjectManagementActionOutcome.Conflict, await store.GetAsync(actionId, cancellationToken));
        }

        try
        {
            AgentToolResult result = await seaTool.ExecutePreparedAsync(new(action.ToolCallId, "sea_manage", "{}"), action.PreparedAction, new(context.TenantId, context.UserId, context.EventId), cancellationToken);
            if (result.IsError) { _ = await store.FailAsync(actionId, new("sea_mutation_failed", "The SEA action could not be completed.", true), cancellationToken); return new(ProjectManagementActionOutcome.Failed, await store.GetAsync(actionId, cancellationToken)); }
            _ = await store.CompleteAsync(actionId, result.Output, cancellationToken);
            return new(ProjectManagementActionOutcome.Succeeded, await store.GetAsync(actionId, cancellationToken));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (UnauthorizedAccessException)
        {
            _ = await store.FailAsync(actionId, new("authorization_revoked", "SEA access was revoked before confirmation and the action was not executed."), cancellationToken);
            return new(ProjectManagementActionOutcome.Forbidden, await store.GetAsync(actionId, cancellationToken));
        }
        catch { _ = await store.FailAsync(actionId, new("unknown_commit_outcome", "The mutation outcome is unknown and will not be retried."), cancellationToken); return new(ProjectManagementActionOutcome.Failed, await store.GetAsync(actionId, cancellationToken)); }
    }
    public async Task<ProjectManagementActionResult> RejectAsync(Guid actionId, ProjectManagementAgentContext context, string? reason, CancellationToken cancellationToken)
    {
        if (reason?.Length > 1_000)
        {
            return new(ProjectManagementActionOutcome.ValidationFailure);
        }

        ProjectManagementAction? action = await store.GetAsync(actionId, cancellationToken);
        if (action is null)
        {
            return new(ProjectManagementActionOutcome.NotFound);
        }

        if (!OwnedBy(action, context))
        {
            return new(ProjectManagementActionOutcome.Forbidden);
        }

        if (action.ExpiresAt <= timeProvider.GetUtcNow()) { _ = await store.ExpireAsync(actionId, cancellationToken); return new(ProjectManagementActionOutcome.Expired, await store.GetAsync(actionId, cancellationToken)); }
        return await store.TryRejectAsync(actionId, string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(), cancellationToken) ? new(ProjectManagementActionOutcome.Succeeded, await store.GetAsync(actionId, cancellationToken)) : new(ProjectManagementActionOutcome.Conflict, await store.GetAsync(actionId, cancellationToken));
    }
    public async Task<ProjectManagementActionResult> GetAsync(Guid actionId, ProjectManagementAgentContext context, CancellationToken cancellationToken)
    { ProjectManagementAction? action = await store.GetAsync(actionId, cancellationToken); return action is null ? new(ProjectManagementActionOutcome.NotFound) : !OwnedBy(action, context) ? new(ProjectManagementActionOutcome.Forbidden) : new(ProjectManagementActionOutcome.Succeeded, action); }
    private static bool OwnedBy(ProjectManagementAction action, ProjectManagementAgentContext context) =>
        context.EventId is { } eventId &&
        string.Equals(action.TenantId, context.TenantId, StringComparison.Ordinal) &&
        string.Equals(action.RequestedByUserId, context.UserId, StringComparison.Ordinal) &&
        string.Equals(action.ScopeKind, "event", StringComparison.Ordinal) &&
        string.Equals(action.SubjectId ?? action.EventId?.ToString("D"), eventId.ToString("D"), StringComparison.Ordinal);
}
