using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;
using Vitarish.Intelligence.Application.Agents;
using Vitarish.Intelligence.Application.GuidedSurfaces;
using Vitarish.Intelligence.Contracts;

namespace Vitarish.Intelligence.Api;

internal static class IntelligenceAgentConversationResultMapper
{
    internal static IResult ToHttpResult(AgentConversationEndpointResult result)
    {
        if (TryToResponse(result, out RunAgentConversationResponse? response))
            return Results.Ok(response);
        (int status, IntelligenceError error) = ToFailure(result);
        return Results.Json(error, statusCode: status);
    }

    /// <summary>The authoritative response of a succeeded turn; false when the result is not a complete success.</summary>
    internal static bool TryToResponse(AgentConversationEndpointResult result, [NotNullWhen(true)] out RunAgentConversationResponse? response)
    {
        if (result.Outcome == AgentConversationOutcome.Succeeded && result.AgentRunId is not null &&
            result.ConversationId.HasValue && result.Result.HasValue)
        {
            response = new RunAgentConversationResponse(
                result.AgentRunId,
                result.ConversationId.Value.ToString("N"),
                result.Result.Value,
                result.Blocks is { Count: > 0 } blocks ? blocks.Select(ToResponse).ToArray() : null);
            return true;
        }
        response = null;
        return false;
    }

    /// <summary>The redacted status and error shared by the buffered response and the streamed Failed frame.</summary>
    internal static (int StatusCode, IntelligenceError Error) ToFailure(AgentConversationEndpointResult result) => result.Outcome switch
    {
        AgentConversationOutcome.NotFound => (StatusCodes.Status404NotFound, new IntelligenceError(IntelligenceErrorCode.NotFound, "The agent conversation was not found.")),
        AgentConversationOutcome.Busy => (StatusCodes.Status409Conflict, new IntelligenceError(IntelligenceErrorCode.Conflict, "The conversation is active.")),
        AgentConversationOutcome.InvalidHistory => (StatusCodes.Status409Conflict, new IntelligenceError(IntelligenceErrorCode.InvalidHistory, "The conversation history is invalid.")),
        AgentConversationOutcome.HistoryLimitExceeded => (StatusCodes.Status409Conflict, new IntelligenceError(IntelligenceErrorCode.InvalidHistory, "The conversation history has reached its supported limit; start a new conversation.")),
        _ => (StatusCodes.Status502BadGateway, new IntelligenceError(IntelligenceErrorCode.UnexpectedFailure, "The agent conversation failed."))
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
        GuidedSurfaceBlockKind.Surface => new(
            GuidedSurfaceBlockKindResponse.Surface,
            Surface: new(block.Surface!.SurfaceId, block.Surface.Protocol, block.Surface.ProtocolVersion, block.Surface.CatalogId, block.Surface.Components, block.Surface.DataModel)),
        _ => throw new ArgumentOutOfRangeException(nameof(block), block.Kind, null)
    };
}
