using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Net.Http.Headers;
using Vitarish.Intelligence.Application.Agents;
using Vitarish.Intelligence.Contracts;
using Vitarish.Modules.Base.Core.Endpoints;

namespace Vitarish.Intelligence.Api;

/// <summary>
/// Writes the frames of one streamed agent turn as NDJSON and flushes each frame. Headers start on
/// <see cref="AcceptedAsync"/>, so a turn rejected before acceptance still gets a standard HTTP error.
/// The writer owns the public sequence, emits exactly one terminal frame, and treats a write that fails
/// or exceeds the write timeout as a disconnected or too slow consumer: it cancels the turn and
/// writes nothing more.
/// </summary>
internal sealed class AgentTurnStreamWriter(
    HttpContext context,
    string operationId,
    CancellationTokenSource turn,
    TimeProvider time,
    TimeSpan writeTimeout) : IAgentTurnObserver
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private const byte LineFeed = (byte)'\n';
    private long sequence;
    private bool terminalWritten;

    internal bool Started { get; private set; }

    internal bool ConsumerFailed { get; private set; }

    // Provisional answer text is not produced by this transport yet; the history-recovery rule reads this.
    public bool AnswerTextEmitted => false;

    public async ValueTask AcceptedAsync(TimeSpan turnDeadline, CancellationToken cancellationToken)
    {
        if (Started)
            return;
        turn.CancelAfter(turnDeadline);
        context.Response.ContentType = ModuleEndpointOpenApiConventions.JsonStream;
        context.Response.Headers.CacheControl = CacheControlHeaderValue.NoStoreString;
        context.Features.Get<IHttpResponseBodyFeature>()?.DisableBuffering();
        Started = true;
        await WriteAsync(StreamChunkKind.Started, null, null);
    }

    public ValueTask StageAsync(AgentTurnStageEvent stage, CancellationToken cancellationToken) =>
        WriteAsync(StreamChunkKind.Progress, new AgentTurnPayload(ToContract(stage.Stage), stage.Ordinal), null);

    internal ValueTask CompleteAsync(RunAgentConversationResponse result) =>
        WriteAsync(StreamChunkKind.Completed, new AgentTurnPayload(Result: result), null);

    internal ValueTask FailAsync(IntelligenceError error) => WriteAsync(StreamChunkKind.Failed, null, error);

    internal ValueTask CancelAsync() => WriteAsync(StreamChunkKind.Canceled, null, null);

    private async ValueTask WriteAsync(StreamChunkKind kind, AgentTurnPayload? payload, IntelligenceError? error)
    {
        if (!Started || ConsumerFailed || terminalWritten)
            return;
        terminalWritten = kind is StreamChunkKind.Completed or StreamChunkKind.Failed or StreamChunkKind.Canceled;
        IntelligenceStreamChunk<AgentTurnPayload> chunk = new(
            kind,
            new IntelligenceStreamMetadata(operationId, sequence++, time.GetUtcNow(), StageOf(kind)),
            payload,
            error);
        byte[] json = JsonSerializer.SerializeToUtf8Bytes(chunk, JsonOptions);
        byte[] line = new byte[json.Length + 1];
        json.CopyTo(line, 0);
        line[^1] = LineFeed;
        try
        {
            // Bounded by the client abort and the write timeout, not by the turn: the terminal
            // frame of a turn cancelled by the server deadline must still be deliverable.
            using CancellationTokenSource write = CancellationTokenSource.CreateLinkedTokenSource(context.RequestAborted);
            write.CancelAfter(writeTimeout);
            await context.Response.Body.WriteAsync(line, write.Token);
            await context.Response.Body.FlushAsync(write.Token);
        }
        catch (Exception exception) when (exception is IOException or ObjectDisposedException or InvalidOperationException or OperationCanceledException)
        {
            // No client transport is left for this or any later frame; stop the turn toward the provider and callbacks.
            ConsumerFailed = true;
            turn.Cancel();
        }
    }

    // Every frame of a live turn reports the responding stage; the terminal kinds report their own.
    private static IntelligenceProgressStage StageOf(StreamChunkKind kind) => kind switch
    {
        StreamChunkKind.Completed => IntelligenceProgressStage.Completed,
        StreamChunkKind.Failed => IntelligenceProgressStage.Failed,
        StreamChunkKind.Canceled => IntelligenceProgressStage.Canceled,
        _ => IntelligenceProgressStage.Responding
    };

    private static AgentTurnProgressStage ToContract(AgentTurnStage stage) => stage switch
    {
        AgentTurnStage.ModelRoundStarted => AgentTurnProgressStage.ModelRoundStarted,
        AgentTurnStage.ToolStarted => AgentTurnProgressStage.ToolStarted,
        AgentTurnStage.ToolCompleted => AgentTurnProgressStage.ToolCompleted,
        AgentTurnStage.ValidatingResult => AgentTurnProgressStage.ValidatingResult,
        AgentTurnStage.SavingConversation => AgentTurnProgressStage.SavingConversation,
        _ => throw new ArgumentOutOfRangeException(nameof(stage), stage, null)
    };
}
