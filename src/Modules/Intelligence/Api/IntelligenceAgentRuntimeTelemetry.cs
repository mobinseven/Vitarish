using Microsoft.Extensions.Logging;

namespace Vitarish.Intelligence.Api;

internal static partial class IntelligenceAgentRuntimeTelemetry
{
    internal const string ToolErrorOutcome = "ToolError";
    internal const string NotFoundOutcome = "NotFound";
    internal const string ForbiddenOutcome = "Forbidden";
    internal const string RejectedOutcome = "Rejected";

    [LoggerMessage(EventId = 73901, Level = LogLevel.Warning,
        Message = "Agent-runtime callback rejected for reason {Reason}; execution {ExecutionId}, agent {AgentKey}.")]
    internal static partial void CallbackRejected(ILogger logger, string reason, string executionId, string agentKey);

    [LoggerMessage(EventId = 73903, Level = LogLevel.Warning,
        Message = "Agent-runtime tool {ToolName} failed with {ExceptionType}; execution {ExecutionId}, agent {AgentKey}.")]
    internal static partial void ToolExecutionFailed(ILogger logger, string toolName, string exceptionType, string executionId, string agentKey);

    [LoggerMessage(EventId = 73904, Level = LogLevel.Information,
        Message = "Agent-runtime tool {ToolName} call {ToolCallId} handled with {Outcome} in {DurationMs} ms; execution {ExecutionId}, agent {AgentKey}.")]
    internal static partial void ToolExecutionHandled(ILogger logger, string toolName, string toolCallId, string outcome, long durationMs, string executionId, string agentKey);

    [LoggerMessage(EventId = 73905, Level = LogLevel.Information,
        Message = "Agent conversation request ended with {Outcome} in {DurationMs} ms, covering authorization, manifest composition and the conversation workflow.")]
    internal static partial void ConversationRequestCompleted(ILogger logger, string outcome, long durationMs);

    [LoggerMessage(EventId = 73906, Level = LogLevel.Error,
        Message = "A streamed agent conversation failed after acceptance with {ExceptionType}.")]
    internal static partial void StreamedTurnFailed(ILogger logger, string exceptionType);
}
