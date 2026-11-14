using System.Text;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Vitarish.Intelligence.Application.Agents;

public sealed record RunAgentConversation(
    Guid? ConversationId,
    AgentConversationScope Scope,
    AgentDefinition Definition,
    AgentInput Input,
    IReadOnlyCollection<IAgentTool> Tools,
    AgentExecutionContext ExecutionContext,
    AgentExecutionLimits Limits,
    Func<Guid, AgentRunResult, CancellationToken, Task<AgentConversationOutcome>>? BeforeCommit = null,
    Func<Guid, AgentRunResult, AgentConversationPresentation?>? Presentation = null,
    string? ProfileCompatibilityKey = null)
    : IRequest<RunAgentConversationResult>;

public sealed record RunAgentConversationResult(
    AgentConversationOutcome Outcome,
    Guid? ConversationId = null,
    AgentRunResult? Run = null);

public sealed record DeleteAgentConversation(
    Guid ConversationId,
    AgentConversationScope Scope)
    : IRequest<AgentConversationOutcome>;

public sealed class RunAgentConversationHandler(
    IAgentConversationStore store,
    IAgentRunner runner,
    TimeProvider? timeProvider = null,
    ILogger<RunAgentConversationHandler>? logger = null)
    : IRequestHandler<RunAgentConversation, RunAgentConversationResult>
{
    public const string HistoryFormat = "openai-agents-js-history/v1";
    public static readonly TimeSpan LeaseSafetyMargin = TimeSpan.FromSeconds(30);

    public async Task<RunAgentConversationResult> Handle(
        RunAgentConversation request,
        CancellationToken cancellationToken)
    {
        TimeSpan runtimeTimeout = request.Limits.EffectiveTimeout;
        AgentConversationStoreResult acquired = request.ProfileCompatibilityKey is { Length: > 0 } key
            ? await store.AcquireAsync(request.ConversationId, request.Scope, runtimeTimeout + LeaseSafetyMargin, key, cancellationToken)
            : await store.AcquireAsync(request.ConversationId, request.Scope, runtimeTimeout + LeaseSafetyMargin, cancellationToken);
        if (logger is not null)
        {
            AgentConversationTelemetry.LeaseOutcome(
                logger,
                acquired.Outcome.ToString(),
                request.Scope.AgentKey,
                request.Scope.SubjectKind,
                request.ConversationId?.ToString("N") ?? "new");
        }
        if (acquired.Outcome != AgentConversationOutcome.Succeeded || acquired.Lease is null)
            return new(acquired.Outcome);

        AgentConversationLease lease = acquired.Lease;
        try
        {
            AgentExecutionContext executionContext = PrepareExecutionContext(
                request.ExecutionContext,
                request.Scope,
                lease.ConversationId,
                (timeProvider ?? TimeProvider.System).GetUtcNow());

            // A conversation always continues. A history produced under another configuration, or one
            // too large to grow, is carried over as its user and assistant messages only.
            string? history = lease.History;
            bool carriedOver = false;
            string? carryReason = lease.ProfileChanged
                ? "profile change"
                : history is not null && Encoding.UTF8.GetByteCount(history) > AgentConversationHistory.CompactionThresholdBytes
                    ? "history size"
                    : null;
            if (carryReason is not null && history is not null)
            {
                history = CarryOver(history, carryReason, executionContext, request.Scope, lease);
                carriedOver = true;
            }

            AgentRunResult run = await runner.RunAsync(
                request.Definition,
                request.Input with { History = history },
                request.Tools,
                executionContext,
                request.Limits,
                cancellationToken);

            // The runtime rejects a history it cannot parse before any tool runs; one attempt on the
            // carried-over history is the only retry, so it does not stack with any transport retry.
            if (!run.IsSuccess && run.Error?.Code == AgentRunErrorCode.InvalidProviderResponse && !carriedOver && history is not null)
            {
                history = CarryOver(history, "rejected history", executionContext, request.Scope, lease);
                run = await runner.RunAsync(
                    request.Definition,
                    request.Input with { History = history },
                    request.Tools,
                    executionContext,
                    request.Limits,
                    cancellationToken);
            }
            if (logger is not null)
                AgentConversationTelemetry.ProviderOutcome(
                    logger,
                    run.IsSuccess ? "succeeded" : run.Error?.Code.ToString() ?? "failed",
                    executionContext.ExecutionId);

            if (!run.IsSuccess)
                return new(
                    run.Error?.Code == AgentRunErrorCode.InvalidProviderResponse
                        ? AgentConversationOutcome.InvalidHistory
                        : AgentConversationOutcome.ProviderFailure,
                    lease.ConversationId,
                    run);
            if (string.IsNullOrWhiteSpace(run.History))
                return new(AgentConversationOutcome.InvalidHistory, lease.ConversationId, run);

            if (request.BeforeCommit is not null)
            {
                AgentConversationOutcome validation = await request.BeforeCommit(lease.ConversationId, run, cancellationToken);
                if (validation != AgentConversationOutcome.Succeeded)
                    return new(validation, lease.ConversationId, run);
            }

            AgentConversationOutcome replaced = await store.ReplaceHistoryAsync(
                lease,
                request.Scope,
                run.History,
                HistoryFormat,
                request.Presentation?.Invoke(lease.ConversationId, run),
                cancellationToken);
            return new(replaced, lease.ConversationId, run);
        }
        finally
        {
            await store.ReleaseAsync(lease, CancellationToken.None);
        }
    }

    private string? CarryOver(
        string history,
        string reason,
        AgentExecutionContext executionContext,
        AgentConversationScope scope,
        AgentConversationLease lease)
    {
        string? carried = AgentConversationHistory.CarryOver(history);
        if (logger is not null)
            AgentConversationTelemetry.HistoryCarriedOver(
                logger,
                reason,
                executionContext.ExecutionId,
                scope.AgentKey,
                lease.ConversationId.ToString("N"),
                carried is null ? 0 : Encoding.UTF8.GetByteCount(carried),
                Encoding.UTF8.GetByteCount(history));
        return carried;
    }

    private static AgentExecutionContext PrepareExecutionContext(
        AgentExecutionContext supplied,
        AgentConversationScope scope,
        Guid conversationId,
        DateTimeOffset now)
    {
        Dictionary<string, string> metadata = supplied.Metadata is null
            ? new(StringComparer.Ordinal)
            : new(supplied.Metadata, StringComparer.Ordinal);

        metadata["tenantId"] = scope.TenantId;
        metadata["practiceId"] = scope.TenantId;
        metadata["subjectKind"] = scope.SubjectKind;
        metadata["subjectId"] = scope.SubjectId;
        metadata["agentKey"] = scope.AgentKey;
        metadata["authenticatedUserId"] = scope.CreatingUserId;
        metadata["patientId"] = scope.SubjectKind == "patient" ? scope.SubjectId : Guid.Empty.ToString("D");
        metadata["conversationId"] = conversationId.ToString("N");
        metadata.TryAdd("permissions", "[]");
        metadata.TryAdd("role", "runtime");
        metadata.TryAdd("locale", "en-US");
        metadata.TryAdd("timeZone", "UTC");
        metadata.TryAdd("calendar", "gregory");
        metadata.TryAdd("currentUtc", now.ToString("O"));
        metadata.TryAdd("currentLocal", now.ToString("O"));
        metadata.TryAdd("correlationId", supplied.ExecutionId);
        return supplied with { Metadata = metadata, Envelope = null };
    }
}

public sealed class DeleteAgentConversationHandler(IAgentConversationStore store)
    : IRequestHandler<DeleteAgentConversation, AgentConversationOutcome>
{
    public Task<AgentConversationOutcome> Handle(
        DeleteAgentConversation request,
        CancellationToken cancellationToken) =>
        store.DeleteAsync(request.ConversationId, request.Scope, cancellationToken);
}
