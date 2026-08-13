using Microsoft.Extensions.Logging;

namespace Vitarish.Clinic.Agents;

internal static partial class ClinicAssistantTelemetry
{
    [LoggerMessage(EventId = 73800, Level = LogLevel.Information,
        Message = "Clinic assistant capability {CapabilityKey} was {Decision}; reason {Reason}.")]
    internal static partial void CapabilityDecision(ILogger logger, string capabilityKey, string decision, string reason);

    [LoggerMessage(EventId = 73801, Level = LogLevel.Information,
        Message = "Clinic assistant conversation ended with {Outcome}; run {AgentRunId}, conversation {ConversationId}.")]
    internal static partial void ConversationCompleted(ILogger logger, string outcome, string agentRunId, string conversationId);

    [LoggerMessage(EventId = 73802, Level = LogLevel.Warning,
        Message = "Clinic assistant provider returned {Outcome}; run {AgentRunId}.")]
    internal static partial void ProviderCompleted(ILogger logger, string outcome, string agentRunId);

    [LoggerMessage(EventId = 73803, Level = LogLevel.Information,
        Message = "Clinic assistant prepared action {ActionType} with action {ActionId}; run {AgentRunId}.")]
    internal static partial void PreparedActionCreated(ILogger logger, string actionType, Guid actionId, string agentRunId);

    [LoggerMessage(EventId = 73804, Level = LogLevel.Warning,
        Message = "Clinic assistant profile compatibility conflict ended run {AgentRunId}, conversation {ConversationId}.")]
    internal static partial void ProfileConflict(ILogger logger, string agentRunId, string conversationId);
}
