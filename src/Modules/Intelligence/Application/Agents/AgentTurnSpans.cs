using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Vitarish.Intelligence.Application.Agents;

/// <summary>
/// Records sequential duration spans for one conversation turn with a monotonic clock. A span reports
/// its outcome and duration also when it is cancelled or faults. It carries no prompt, answer, tool
/// payload or credential, and its correlation values are log fields only.
/// </summary>
internal sealed class AgentTurnSpans(ILogger? logger, string executionId)
{
    internal const string SucceededOutcome = "Succeeded";
    internal const string CancelledOutcome = "Cancelled";
    internal const string FaultedOutcome = "Faulted";

    private int attempts;

    internal async Task<T> MeasureAsync<T>(AgentTurnPhase phase, Func<Task<T>> work, Func<T, string> outcomeOf)
    {
        long startedAt = Stopwatch.GetTimestamp();
        string outcome = FaultedOutcome;
        try
        {
            T result = await work();
            outcome = outcomeOf(result);
            return result;
        }
        catch (OperationCanceledException)
        {
            outcome = CancelledOutcome;
            throw;
        }
        finally
        {
            Record(phase, outcome, startedAt);
        }
    }

    internal T Measure<T>(AgentTurnPhase phase, Func<T> work)
    {
        long startedAt = Stopwatch.GetTimestamp();
        string outcome = FaultedOutcome;
        try
        {
            T result = work();
            outcome = SucceededOutcome;
            return result;
        }
        finally
        {
            Record(phase, outcome, startedAt);
        }
    }

    /// <summary>Runs one runtime attempt, recording its duration, outcome, call counts and reported tokens.</summary>
    internal async Task<AgentRunResult> MeasureRuntimeAttemptAsync(Func<Task<AgentRunResult>> work)
    {
        int attempt = ++attempts;
        long startedAt = Stopwatch.GetTimestamp();
        string outcome = FaultedOutcome;
        AgentRunResult? run = null;
        try
        {
            run = await work();
            outcome = OutcomeOf(run);
            return run;
        }
        catch (OperationCanceledException)
        {
            outcome = CancelledOutcome;
            throw;
        }
        finally
        {
            if (logger is not null)
                AgentConversationTelemetry.RuntimeAttemptCompleted(
                    logger,
                    attempt,
                    outcome,
                    ElapsedMs(startedAt),
                    executionId,
                    run?.Iterations ?? 0,
                    run?.ToolCalls ?? 0,
                    run?.Usage.InputTokens ?? 0,
                    run?.Usage.OutputTokens ?? 0);
        }
    }

    internal void CompleteWorkflow(string outcome, long startedAt)
    {
        if (logger is not null)
            AgentConversationTelemetry.WorkflowCompleted(logger, outcome, ElapsedMs(startedAt), executionId, attempts);
    }

    internal static string OutcomeOf(AgentRunResult run) =>
        run.IsSuccess ? SucceededOutcome : run.Error?.Code.ToString() ?? "Failed";

    private void Record(AgentTurnPhase phase, string outcome, long startedAt)
    {
        if (logger is not null)
            AgentConversationTelemetry.PhaseCompleted(logger, phase, outcome, ElapsedMs(startedAt), executionId);
    }

    private static long ElapsedMs(long startedAt) => (long)Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds;
}
