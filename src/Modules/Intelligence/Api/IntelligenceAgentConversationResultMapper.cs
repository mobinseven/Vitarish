using Microsoft.AspNetCore.Http;
using Vitarish.Intelligence.Application.Agents;
using Vitarish.Intelligence.Application.GuidedSurfaces;
using Vitarish.Intelligence.Contracts;

namespace Vitarish.Intelligence.Api;

internal static class IntelligenceAgentConversationResultMapper
{
    internal static IResult ToHttpResult(AgentConversationEndpointResult result) => result.Outcome switch
    {
        AgentConversationOutcome.Succeeded when result.AgentRunId is not null && result.ConversationId.HasValue && result.Result.HasValue =>
            Results.Ok(new RunAgentConversationResponse(
                result.AgentRunId,
                result.ConversationId.Value.ToString("N"),
                result.Result.Value,
                result.Blocks is { Count: > 0 } blocks ? blocks.Select(ToResponse).ToArray() : null)),
        AgentConversationOutcome.NotFound => Results.NotFound(new IntelligenceError(IntelligenceErrorCode.NotFound, "The agent conversation was not found.")),
        AgentConversationOutcome.Busy => Results.Conflict(new IntelligenceError(IntelligenceErrorCode.Conflict, "The conversation is active.")),
        AgentConversationOutcome.InvalidHistory => Results.Conflict(new IntelligenceError(IntelligenceErrorCode.InvalidHistory, "The conversation history is invalid.")),
        AgentConversationOutcome.HistoryLimitExceeded => Results.Conflict(new IntelligenceError(IntelligenceErrorCode.InvalidHistory, "The conversation history has reached its supported limit; start a new conversation.")),
        AgentConversationOutcome.ProfileChanged => Results.Conflict(new IntelligenceError(IntelligenceErrorCode.Conflict, "The agent profile changed; start a new conversation.")),
        _ => Results.Json(new IntelligenceError(IntelligenceErrorCode.UnexpectedFailure, "The agent conversation failed."), statusCode: StatusCodes.Status502BadGateway)
    };

    internal static GuidedSurfaceBlockResponse ToResponse(GuidedSurfaceBlock block) => block.Kind switch
    {
        GuidedSurfaceBlockKind.Question => new(
            GuidedSurfaceBlockKindResponse.Question,
            Question: new(
                block.Question!.Prompt,
                block.Question.Options.Select(option => new GuidedSurfaceOptionResponse(option.Key, option.Label)).ToArray(),
                block.Question.MaxSelections)),
        GuidedSurfaceBlockKind.NextAction => new(GuidedSurfaceBlockKindResponse.NextAction, NextAction: block.NextAction),
        GuidedSurfaceBlockKind.ActionGroup => new(
            GuidedSurfaceBlockKindResponse.ActionGroup,
            ActionGroup: new(block.ActionGroup!.AgentRunId, block.ActionGroup.ActionIds)),
        _ => throw new ArgumentOutOfRangeException(nameof(block), block.Kind, null)
    };
}
