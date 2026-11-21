using Microsoft.Extensions.Logging;

namespace Vitarish.Intelligence.Application.Agents;

internal static partial class AgentConversationTelemetry
{
    [LoggerMessage(EventId = 73601, Level = LogLevel.Information,
        Message = "Agent conversation lease acquisition ended with {Outcome}; agent {AgentKey}, subject kind {SubjectKind}, conversation {ConversationId}.")]
    internal static partial void LeaseOutcome(ILogger logger, string outcome, string agentKey, string subjectKind, string conversationId);

    [LoggerMessage(EventId = 73602, Level = LogLevel.Information,
        Message = "Agent provider execution ended with {Outcome}; execution {ExecutionId}.")]
    internal static partial void ProviderOutcome(ILogger logger, string outcome, string executionId);

    [LoggerMessage(EventId = 73604, Level = LogLevel.Information,
        Message = "Agent conversation history carried over for {Reason}; execution {ExecutionId}, agent {AgentKey}, conversation {ConversationId}, carried {CarriedBytes} of {StoredBytes} bytes.")]
    internal static partial void HistoryCarriedOver(ILogger logger, string reason, string executionId, string agentKey, string conversationId, int carriedBytes, int storedBytes);

    [LoggerMessage(EventId = 73605, Level = LogLevel.Information,
        Message = "Agent turn phase {Phase} ended with {Outcome} in {DurationMs} ms; execution {ExecutionId}.")]
    internal static partial void PhaseCompleted(ILogger logger, AgentTurnPhase phase, string outcome, long durationMs, string executionId);

    [LoggerMessage(EventId = 73606, Level = LogLevel.Information,
        Message = "Agent runtime attempt {Attempt} ended with {Outcome} in {DurationMs} ms; execution {ExecutionId}, model rounds {ModelRounds}, tool calls {ToolCalls}, input tokens {InputTokens}, output tokens {OutputTokens}.")]
    internal static partial void RuntimeAttemptCompleted(ILogger logger, int attempt, string outcome, long durationMs, string executionId, int modelRounds, int toolCalls, int inputTokens, int outputTokens);

    [LoggerMessage(EventId = 73607, Level = LogLevel.Information,
        Message = "Agent conversation workflow ended with {Outcome} in {DurationMs} ms; execution {ExecutionId}, attempts {Attempts}.")]
    internal static partial void WorkflowCompleted(ILogger logger, string outcome, long durationMs, string executionId, int attempts);
}
