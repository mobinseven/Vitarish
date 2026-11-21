using System.Diagnostics;
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
    string? ProfileCompatibilityKey = null,
    IAgentTurnObserver? Observer = null,
    AgentAnswerStream? AnswerStream = null)
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

    /// <summary>The server-side turn bound beyond the runtime timeout, covering validation and persistence.</summary>
    public static readonly TimeSpan TurnDeadlineMargin = TimeSpan.FromSeconds(10);

    public async Task<RunAgentConversationResult> Handle(
        RunAgentConversation request,
        CancellationToken cancellationToken)
    {
        AgentTurnSpans spans = new(logger, request.ExecutionContext.ExecutionId);
        long startedAt = Stopwatch.GetTimestamp();
        string outcome = AgentTurnSpans.FaultedOutcome;
        try
        {
            RunAgentConversationResult result = await HandleSpannedAsync(request, spans, cancellationToken);
            outcome = result.Outcome == AgentConversationOutcome.ProviderFailure && result.Run?.Error is { } error
                ? error.Code.ToString()
                : result.Outcome.ToString();
            return result;
        }
        catch (OperationCanceledException)
        {
            outcome = AgentTurnSpans.CancelledOutcome;
            throw;
        }
        finally
        {
            spans.CompleteWorkflow(outcome, startedAt);
        }
    }

    private async Task<RunAgentConversationResult> HandleSpannedAsync(
        RunAgentConversation request,
        AgentTurnSpans spans,
        CancellationToken cancellationToken)
    {
        TimeSpan runtimeTimeout = request.Limits.EffectiveTimeout;
        AgentConversationStoreResult acquired = await spans.MeasureAsync(
            AgentTurnPhase.LeaseAcquire,
            () => request.ProfileCompatibilityKey is { Length: > 0 } key
                ? store.AcquireAsync(request.ConversationId, request.Scope, runtimeTimeout + LeaseSafetyMargin, key, cancellationToken)
                : store.AcquireAsync(request.ConversationId, request.Scope, runtimeTimeout + LeaseSafetyMargin, cancellationToken),
            result => result.Outcome.ToString());
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
        IAgentTurnObserver? observer = request.Observer;
        try
        {
            if (observer is not null)
                await observer.AcceptedAsync(runtimeTimeout + TurnDeadlineMargin, cancellationToken);
            (AgentExecutionContext executionContext, string? history, bool carriedOver) = spans.Measure(
                AgentTurnPhase.HistoryPreparation,
                () => PrepareTurn(request, lease));

            AgentRunResult run = await spans.MeasureRuntimeAttemptAsync(() => RunAttemptAsync(
                request, history, executionContext, observer, cancellationToken));

            // A rejected history is retried once on its carried-over messages, and only when the runtime
            // reports that no tool ran, so no tool (including a direct mutation) executes twice. This is
            // the only retry; it does not stack with any transport retry. A streamed turn that already
            // showed provisional answer text fails instead, so no new attempt replaces what was seen.
            if (!run.IsSuccess && run.Error?.Code == AgentRunErrorCode.InvalidProviderResponse &&
                run.ToolCalls == 0 && run.ToolExecutions.Count == 0 && !carriedOver && history is not null &&
                observer is not { AnswerTextEmitted: true })
            {
                history = CarryOver(history, "rejected history", executionContext, request.Scope, lease);
                string? retryHistory = history;
                run = await spans.MeasureRuntimeAttemptAsync(() => RunAttemptAsync(
                    request, retryHistory, executionContext, observer, cancellationToken));
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
                if (observer is not null)
                    await observer.StageAsync(new(AgentTurnStage.ValidatingResult), cancellationToken);
                AgentConversationOutcome validation = await spans.MeasureAsync(
                    AgentTurnPhase.OutputValidation,
                    () => request.BeforeCommit(lease.ConversationId, run, cancellationToken),
                    outcome => outcome.ToString());
                if (validation != AgentConversationOutcome.Succeeded)
                    return new(validation, lease.ConversationId, run);
            }

            string persistedHistory = run.History;
            if (observer is not null)
                await observer.StageAsync(new(AgentTurnStage.SavingConversation), cancellationToken);
            AgentConversationOutcome replaced = await spans.MeasureAsync(
                AgentTurnPhase.Persistence,
                () => store.ReplaceHistoryAsync(
                    lease,
                    request.Scope,
                    persistedHistory,
                    HistoryFormat,
                    request.Presentation?.Invoke(lease.ConversationId, run),
                    cancellationToken),
                outcome => outcome.ToString());
            return new(replaced, lease.ConversationId, run);
        }
        finally
        {
            await spans.MeasureAsync(
                AgentTurnPhase.LeaseRelease,
                async () =>
                {
                    await store.ReleaseAsync(lease, CancellationToken.None);
                    return true;
                },
                _ => AgentTurnSpans.SucceededOutcome);
        }
    }

    private Task<AgentRunResult> RunAttemptAsync(
        RunAgentConversation request,
        string? history,
        AgentExecutionContext executionContext,
        IAgentTurnObserver? observer,
        CancellationToken cancellationToken) => observer is null
        ? runner.RunAsync(
            request.Definition,
            request.Input with { History = history },
            request.Tools,
            executionContext,
            request.Limits,
            cancellationToken)
        : runner.RunStreamedAsync(
            request.Definition,
            request.Input with { History = history },
            request.Tools,
            executionContext,
            request.Limits,
            request.AnswerStream,
            observer,
            cancellationToken);

    private (AgentExecutionContext ExecutionContext, string? History, bool CarriedOver) PrepareTurn(
        RunAgentConversation request,
        AgentConversationLease lease)
    {
        AgentExecutionContext executionContext = PrepareExecutionContext(
            request.ExecutionContext,
            request.Scope,
            lease.ConversationId,
            (timeProvider ?? TimeProvider.System).GetUtcNow());

        // A conversation always continues. A history produced under another configuration, or one
        // too large to grow, is carried over as its user and assistant messages only.
        string? history = lease.History;
        string? carryReason = lease.ProfileChanged
            ? "profile change"
            : history is not null && Encoding.UTF8.GetByteCount(history) > AgentConversationHistory.CompactionThresholdBytes
                ? "history size"
                : null;
        if (carryReason is null || history is null)
            return (executionContext, history, false);
        return (executionContext, CarryOver(history, carryReason, executionContext, request.Scope, lease), true);
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
