using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Vitarish.Intelligence.Application.Abstractions;
using Vitarish.Intelligence.Contracts;
using Vitarish.Modules.Base.Endpoints;
using D = Vitarish.Intelligence.Domain.Models;

namespace Vitarish.Intelligence.Api;

public static class IntelligenceApiEndpointMapping
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static IEndpointRouteBuilder MapIntelligenceApi(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints.MapProtectedModuleGroup(IntelligenceApiRoutes.Base);
        _ = group.MapPost(IntelligenceApiRoutes.Responder, RespondAsync);
        _ = group.MapPost(IntelligenceApiRoutes.ResponderStream, StreamRespondAsync);
        _ = group.MapPost(IntelligenceApiRoutes.Transcription, TranscribeAsync);
        _ = group.MapPost(IntelligenceApiRoutes.TranscriptionStream, StreamTranscribeAsync);
        _ = group.MapPost(IntelligenceApiRoutes.Rewrite, RewriteAsync);
        _ = group.MapPost(IntelligenceApiRoutes.RewriteStream, StreamRewriteAsync);
        return endpoints;
    }

    private static async Task<IResult> RespondAsync(ResponderRequest request, IResponder responder, CancellationToken ct)
    {
        IReadOnlyList<D.ResponderOutput> outputs = await responder.GenerateAsync(request.Inputs.Select(ToDomain).ToArray(), ToDomain(request.Options), ct).ConfigureAwait(false);
        return Results.Ok(new ResponderResult(outputs.Select(ToContract).ToArray(), request.Context));
    }

    private static async Task StreamRespondAsync(ResponderRequest request, IResponder responder, HttpContext context, CancellationToken ct)
    {
        context.Response.ContentType = "application/x-ndjson";
        string operationId = request.Context?.OperationId ?? Guid.NewGuid().ToString("N");
        long seq = 0;
        await Write(context, new IntelligenceStreamChunk<ResponderProgress>(StreamChunkKind.Started, Meta(operationId, seq++, IntelligenceProgressStage.Responding, request.Context), new ResponderProgress(IntelligenceProgressStage.Responding)), ct);
        await foreach (D.ResponderOutput output in responder.StreamAsync(request.Inputs.Select(ToDomain).ToArray(), ToDomain(request.Options), ct).WithCancellation(ct).ConfigureAwait(false))
        {
            await Write(context, new IntelligenceStreamChunk<ResponderProgress>(StreamChunkKind.Delta, Meta(operationId, seq++, IntelligenceProgressStage.Responding, request.Context), new ResponderProgress(IntelligenceProgressStage.Responding, ToContract(output))), ct);
        }
        await Write(context, new IntelligenceStreamChunk<ResponderProgress>(StreamChunkKind.Completed, Meta(operationId, seq, IntelligenceProgressStage.Completed, request.Context), new ResponderProgress(IntelligenceProgressStage.Completed)), ct);
    }

    private static Task<IResult> TranscribeAsync(TranscriptionRequest request, CancellationToken ct) => Task.FromResult<IResult>(Results.BadRequest(IntelligenceResult<TranscriptionResult>.Failed(new IntelligenceError(IntelligenceErrorCode.UnsupportedMediaType, "Direct Intelligence transcription upload is not approved; provide Files-owned references to a backend-approved transcription workflow."))));
    private static async Task StreamTranscribeAsync(TranscriptionRequest request, HttpContext context, CancellationToken ct) { context.Response.ContentType = "application/x-ndjson"; await Write(context, new IntelligenceStreamChunk<TranscriptionProgress>(StreamChunkKind.Failed, Meta(request.Context?.OperationId ?? Guid.NewGuid().ToString("N"), 0, IntelligenceProgressStage.Failed, request.Context), new TranscriptionProgress(IntelligenceProgressStage.Failed), new IntelligenceError(IntelligenceErrorCode.UnsupportedMediaType, "Transcription streaming upload is deferred by the accepted ADR.")), ct); }

    private static async Task<IResult> RewriteAsync(RewriteRequest request, IResponder responder, CancellationToken ct)
    {
        D.ResponderInput[] input = [D.ResponderInput.TextInput(request.Prompt)];
        IReadOnlyList<D.ResponderOutput> outputs = await responder.GenerateAsync(input, new D.ResponderOptions { Instructions = request.Options?.Instructions, Model = request.Options?.ModelId }, ct).ConfigureAwait(false);
        return Results.Ok(new RewriteResult(string.Concat(outputs.Where(o => o.Type == D.ResponderOutputType.Text).Select(o => o.Text)), request.Context));
    }

    private static async Task StreamRewriteAsync(RewriteRequest request, IResponder responder, HttpContext context, CancellationToken ct)
    {
        context.Response.ContentType = "application/x-ndjson";
        string operationId = request.Context?.OperationId ?? Guid.NewGuid().ToString("N");
        long seq = 0;
        await foreach (D.ResponderOutput output in responder.StreamAsync([D.ResponderInput.TextInput(request.Prompt)], new D.ResponderOptions { Instructions = request.Options?.Instructions, Model = request.Options?.ModelId }, ct).WithCancellation(ct).ConfigureAwait(false))
        {
            await Write(context, new IntelligenceStreamChunk<RewriteProgress>(StreamChunkKind.Delta, Meta(operationId, seq++, IntelligenceProgressStage.Rewriting, request.Context), new RewriteProgress(IntelligenceProgressStage.Rewriting, output.Text)), ct);
        }
        await Write(context, new IntelligenceStreamChunk<RewriteProgress>(StreamChunkKind.Completed, Meta(operationId, seq, IntelligenceProgressStage.Completed, request.Context), new RewriteProgress(IntelligenceProgressStage.Completed)), ct);
    }

    private static IntelligenceStreamMetadata Meta(string operationId, long sequence, IntelligenceProgressStage stage, IntelligenceSessionContext? context) => new(operationId, sequence, DateTimeOffset.UtcNow, stage, context);
    private static async Task Write<T>(HttpContext context, IntelligenceStreamChunk<T> chunk, CancellationToken ct) { await context.Response.WriteAsync(JsonSerializer.Serialize(chunk, JsonOptions), ct); await context.Response.WriteAsync("\n", ct); await context.Response.Body.FlushAsync(ct); }
    private static D.ResponderInput ToDomain(ResponderInput input) => input.Type switch { ResponderInputType.Image => D.ResponderInput.ImageInput(input.ImageUrl ?? string.Empty, (D.ResponderInputImageDetail)(int)(input.Detail ?? ResponderInputImageDetail.Auto)), ResponderInputType.File => D.ResponderInput.FileInput(input.File?.FileId ?? string.Empty, input.File?.TemporaryUploadReference ?? string.Empty, input.File?.FileName ?? string.Empty), _ => D.ResponderInput.TextInput(input.Text ?? string.Empty) };
    private static D.ResponderOptions ToDomain(ResponderOptions? options) => new() { Instructions = options?.Instructions, Model = options?.ModelId };
    private static ResponderOutput ToContract(D.ResponderOutput output) => new() { Type = output.Type == D.ResponderOutputType.Text ? ResponderOutputType.Text : ResponderOutputType.Json, Text = output.Text };
}
