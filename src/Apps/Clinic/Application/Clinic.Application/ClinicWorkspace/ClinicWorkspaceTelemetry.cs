using Microsoft.Extensions.Logging;

namespace Vitarish.Clinic.Application.ClinicWorkspace;

internal static partial class ClinicWorkspaceTelemetry
{
    [LoggerMessage(
        EventId = 73701,
        Level = LogLevel.Information,
        Message = "Clinic agent tool {ToolName} in capability {Capability} completed with {Outcome} in {ElapsedMilliseconds}ms; run {AgentRunId}, call {ToolCallId}.")]
    internal static partial void ToolCompleted(
        ILogger logger,
        string toolName,
        string capability,
        string outcome,
        long elapsedMilliseconds,
        string agentRunId,
        string toolCallId);

    [LoggerMessage(
        EventId = 73702,
        Level = LogLevel.Warning,
        Message = "Clinic agent tool {ToolName} in capability {Capability} failed in {ElapsedMilliseconds}ms; run {AgentRunId}, call {ToolCallId}.")]
    internal static partial void ToolFailed(
        ILogger logger,
        string toolName,
        string capability,
        long elapsedMilliseconds,
        string agentRunId,
        string toolCallId);

    [LoggerMessage(
        EventId = 73703,
        Level = LogLevel.Information,
        Message = "Clinic agent action {ActionType} completed with {Outcome}; run {AgentRunId}, action {ActionId}.")]
    internal static partial void ActionCompleted(
        ILogger logger,
        string actionType,
        string outcome,
        string agentRunId,
        Guid actionId);
}
