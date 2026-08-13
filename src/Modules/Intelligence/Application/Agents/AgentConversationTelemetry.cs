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

    [LoggerMessage(EventId = 73603, Level = LogLevel.Warning,
        Message = "Agent profile compatibility conflict rejected execution {ExecutionId}; agent {AgentKey}, conversation {ConversationId}.")]
    internal static partial void ProfileConflict(ILogger logger, string executionId, string agentKey, string conversationId);
}
