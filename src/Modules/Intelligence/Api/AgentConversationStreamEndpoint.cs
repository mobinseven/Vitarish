using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Vitarish.Intelligence.Application.Agents;
using Vitarish.Intelligence.Contracts;

namespace Vitarish.Intelligence.Api;

/// <summary>
/// The streamed twin of the buffered agent conversation route. It resolves the same app-owned
/// endpoint and so runs the same conversation workflow; it only adds an observer that reports trusted
/// stages and writes the terminal frame after the workflow's own result and persistence checks.
/// </summary>
internal static class AgentConversationStreamEndpoint
{
    internal static readonly TimeSpan FrameWriteTimeout = TimeSpan.FromSeconds(15);

    internal static async Task<IResult> StreamAsync(
        RunAgentConversationRequest request,
        IAgentConversationEndpointRegistry endpoints,
        ILogger logger,
        TimeProvider time,
        HttpContext context,
        TimeSpan writeTimeout,
        CancellationToken ct)
    {
        if (IntelligenceApiEndpointMapping.RejectInvalidConversationRequest(request) is { } rejected)
            return rejected;
        AgentConversationRegistration? registration = endpoints.ResolveRegistration(request.AgentKey, request.SubjectKind);
        if (registration is null)
            return Results.NotFound(new IntelligenceError(IntelligenceErrorCode.NotFound, "The agent conversation route was not found."));
        // Checked before any execution so a client can choose the buffered route; it is never replayed for it.
        if (!registration.Route.StreamingEnabled)
            return Results.Conflict(new IntelligenceError(IntelligenceErrorCode.StreamingNotEnabled, "Streaming is not enabled for this agent; use the buffered conversation route."));

        using CancellationTokenSource turn = CancellationTokenSource.CreateLinkedTokenSource(ct);
        AgentTurnStreamWriter writer = new(context, Guid.NewGuid().ToString("N"), turn, time, writeTimeout);
        long acceptedAt = Stopwatch.GetTimestamp();
        string outcome = IntelligenceAgentRuntimeTelemetry.FaultedOutcome;
        AgentConversationEndpointResult? result = null;
        bool cancelled = false;
        try
        {
            try
            {
                result = await registration.Endpoint.RunAsync(
                    new(request.AgentKey, request.SubjectKind, request.SubjectId, request.Message, request.ConversationId, writer),
                    turn.Token);
            }
            catch (OperationCanceledException)
            {
                cancelled = true;
            }
            catch (Exception exception) when (writer.Started)
            {
                IntelligenceAgentRuntimeTelemetry.StreamedTurnFailed(logger, exception.GetType().Name);
                await writer.FailAsync(new IntelligenceError(IntelligenceErrorCode.UnexpectedFailure, "The agent conversation failed."));
                return Results.Empty;
            }

            if (ct.IsCancellationRequested)
            {
                // The client is gone: nothing can be delivered. The turn was cancelled toward the runtime and its lease released.
                outcome = IntelligenceAgentRuntimeTelemetry.CancelledOutcome;
                throw new OperationCanceledException(ct);
            }
            if (writer.ConsumerFailed)
            {
                outcome = IntelligenceAgentRuntimeTelemetry.CancelledOutcome;
                context.Abort();
                return Results.Empty;
            }
            if (!writer.Started)
            {
                // Rejected before acceptance (authorization, tenant, busy, missing): a standard HTTP error, no stream.
                if (result is null)
                    throw new OperationCanceledException(turn.Token);
                outcome = result.Outcome.ToString();
                return IntelligenceAgentConversationResultMapper.ToHttpResult(result);
            }

            // On an intact connection the workflow ended after its own result and persistence checks.
            if (cancelled || result is null)
            {
                outcome = IntelligenceAgentRuntimeTelemetry.CancelledOutcome;
                await writer.CancelAsync();
            }
            else if (IntelligenceAgentConversationResultMapper.TryToResponse(result, out RunAgentConversationResponse? response))
            {
                outcome = result.Outcome.ToString();
                await writer.CompleteAsync(response);
            }
            else
            {
                outcome = result.Outcome.ToString();
                (int status, IntelligenceError error) = IntelligenceAgentConversationResultMapper.ToFailure(result);
                await writer.FailAsync(error with { StatusCode = status });
            }
            return Results.Empty;
        }
        finally
        {
            IntelligenceAgentRuntimeTelemetry.ConversationRequestCompleted(
                logger,
                outcome,
                (long)Stopwatch.GetElapsedTime(acceptedAt).TotalMilliseconds);
        }
    }
}
