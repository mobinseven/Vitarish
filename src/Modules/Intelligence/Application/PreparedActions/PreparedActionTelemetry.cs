using Microsoft.Extensions.Logging;

namespace Vitarish.Intelligence.Application.PreparedActions;

internal static partial class PreparedActionTelemetry
{
    [LoggerMessage(EventId = 73610, Level = LogLevel.Information,
        Message = "Prepared action {ActionType} of agent {AgentKey} ended with {Outcome}; run {AgentRunId}, action {ActionId}.")]
    internal static partial void Completed(ILogger logger, string actionType, string agentKey, string outcome, string agentRunId, Guid actionId);

    [LoggerMessage(EventId = 73611, Level = LogLevel.Information,
        Message = "Prepared action {ActionType} confirmation claim returned {Outcome}; action {ActionId}.")]
    internal static partial void ConfirmationClaimed(ILogger logger, string actionType, string outcome, Guid actionId);

    [LoggerMessage(EventId = 73612, Level = LogLevel.Information,
        Message = "Prepared action {ActionType} confirmation retry classified as {Classification}; action {ActionId}.")]
    internal static partial void Retry(ILogger logger, string actionType, string classification, Guid actionId);

    [LoggerMessage(EventId = 73613, Level = LogLevel.Warning,
        Message = "Prepared action {ActionType} confirmation failed; action {ActionId}, retryable {Retryable}.")]
    internal static partial void Failed(ILogger logger, string actionType, Guid actionId, bool retryable);

    [LoggerMessage(EventId = 73614, Level = LogLevel.Information,
        Message = "Prepared action {ActionType} of agent {AgentKey} was prepared; run {AgentRunId}, action {ActionId}.")]
    internal static partial void Prepared(ILogger logger, string actionType, string agentKey, string agentRunId, Guid actionId);
}
