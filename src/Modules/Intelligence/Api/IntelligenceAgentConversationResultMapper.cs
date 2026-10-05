using Microsoft.AspNetCore.Http;
using Vitarish.Intelligence.Application.Agents;
using Vitarish.Intelligence.Contracts;

namespace Vitarish.Intelligence.Api;

internal static class IntelligenceAgentConversationResultMapper
{
    internal static IResult ToHttpResult(AgentConversationEndpointResult result) => result.Outcome switch
    {
        AgentConversationOutcome.Succeeded when result.AgentRunId is not null && result.ConversationId.HasValue && result.Result.HasValue =>
            Results.Ok(new RunAgentConversationResponse(result.AgentRunId, result.ConversationId.Value.ToString("N"), result.Result.Value)),
        AgentConversationOutcome.NotFound => Results.NotFound(new IntelligenceError(IntelligenceErrorCode.NotFound, "The agent conversation was not found.")),
        AgentConversationOutcome.Busy => Results.Conflict(new IntelligenceError(IntelligenceErrorCode.Conflict, "The conversation is active.")),
        AgentConversationOutcome.InvalidHistory => Results.Conflict(new IntelligenceError(IntelligenceErrorCode.InvalidHistory, "The conversation history is invalid.")),
        AgentConversationOutcome.HistoryLimitExceeded => Results.Conflict(new IntelligenceError(IntelligenceErrorCode.InvalidHistory, "The conversation history has reached its supported limit; start a new conversation.")),
        AgentConversationOutcome.ProfileChanged => Results.Conflict(new IntelligenceError(IntelligenceErrorCode.Conflict, "The agent profile changed; start a new conversation.")),
        _ => Results.Json(new IntelligenceError(IntelligenceErrorCode.UnexpectedFailure, "The agent conversation failed."), statusCode: StatusCodes.Status502BadGateway)
    };
}
