using Microsoft.Extensions.Logging;

namespace Vitarish.Clinic.Application.Assistant;

internal static partial class AssistantTelemetry
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

    [LoggerMessage(EventId = 73704, Level = LogLevel.Warning,
        Message = "Clinic agent tool {ToolName} was rejected in capability {Capability} for {Reason}; run {AgentRunId}, call {ToolCallId}.")]
    internal static partial void ToolRejected(ILogger logger, string toolName, string capability, string reason, string agentRunId, string toolCallId);

    [LoggerMessage(EventId = 73706, Level = LogLevel.Information,
        Message = "Clinic agent composition produced {Outcome} for subject kind {SubjectKind} with {CapabilityCount} capabilities and {ToolCount} tools; compatibility {CompatibilityKey}.")]
    internal static partial void CompositionCompleted(ILogger logger, string outcome, string subjectKind, int capabilityCount, int toolCount, string compatibilityKey);

    [LoggerMessage(EventId = 73707, Level = LogLevel.Warning,
        Message = "Clinic agent conversation ended with {Outcome}; run {AgentRunId}, conversation {ConversationId}.")]
    internal static partial void ConversationCompleted(ILogger logger, string outcome, string agentRunId, string conversationId);
}
