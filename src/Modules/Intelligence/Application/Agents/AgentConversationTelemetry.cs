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
}
