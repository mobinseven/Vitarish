using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;
using Vitarish.Intelligence.Application.Agents;
using Vitarish.Intelligence.Application.GuidedSurfaces;
using Vitarish.Intelligence.Contracts;

namespace Vitarish.Intelligence.Api;

internal static class IntelligenceAgentConversationResultMapper
{
    /// <summary>True when the request continues an existing conversation rather than starting one.</summary>
    internal static bool IsResume(RunAgentConversationRequest request) => !string.IsNullOrWhiteSpace(request.ConversationId);

    internal static IResult ToHttpResult(AgentConversationEndpointResult result, bool resumedConversation)
    {
        if (TryToResponse(result, out RunAgentConversationResponse? response))
            return Results.Ok(response);
        (int status, IntelligenceError error) = ToFailure(result, resumedConversation);
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

    /// <summary>
    /// True when the turn ended by the server deadline or a server-initiated cancel. A streamed turn on an
    /// intact connection reports these as a Canceled frame; buffered responses keep the generic failure.
    /// </summary>
    internal static bool IsCancellation(AgentConversationEndpointResult result) =>
        result.Outcome is AgentConversationOutcome.TimedOut or AgentConversationOutcome.Cancelled;

    /// <summary>
    /// The redacted status and error shared by the buffered response and the streamed Failed frame. A not-found
    /// outcome of a request that named a conversation is <see cref="IntelligenceErrorCode.ConversationNotFound"/>:
    /// the one signal that the conversation is missing, expired or not visible to this caller. A missing and a
    /// concealed conversation are indistinguishable by design, and an unknown route keeps
    /// <see cref="IntelligenceErrorCode.NotFound"/>. The status stays 404 either way.
    /// </summary>
    internal static (int StatusCode, IntelligenceError Error) ToFailure(AgentConversationEndpointResult result, bool resumedConversation) => result.Outcome switch
    {
        AgentConversationOutcome.NotFound when resumedConversation => (StatusCodes.Status404NotFound, new IntelligenceError(IntelligenceErrorCode.ConversationNotFound, "The agent conversation was not found.")),
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
