using Microsoft.Extensions.Logging;

namespace Vitarish.Clinic.Api;

internal static partial class AssistantCallbackTelemetry
{
    [LoggerMessage(EventId = 73902, Level = LogLevel.Warning,
        Message = "Clinic agent-runtime callback rejected for reason {Reason}; execution {ExecutionId}, agent {AgentKey}.")]
    internal static partial void CallbackRejected(ILogger logger, string reason, string executionId, string agentKey);
}
