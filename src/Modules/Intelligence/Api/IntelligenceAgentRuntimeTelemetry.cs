using Microsoft.Extensions.Logging;

namespace Vitarish.Intelligence.Api;

internal static partial class IntelligenceAgentRuntimeTelemetry
{
    [LoggerMessage(EventId = 73901, Level = LogLevel.Warning,
        Message = "Agent-runtime callback rejected for reason {Reason}; execution {ExecutionId}, agent {AgentKey}.")]
    internal static partial void CallbackRejected(ILogger logger, string reason, string executionId, string agentKey);

    [LoggerMessage(EventId = 73903, Level = LogLevel.Warning,
        Message = "Agent-runtime tool {ToolName} failed with {ExceptionType}; execution {ExecutionId}, agent {AgentKey}.")]
    internal static partial void ToolExecutionFailed(ILogger logger, string toolName, string exceptionType, string executionId, string agentKey);
}
