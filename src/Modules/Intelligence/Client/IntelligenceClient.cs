using System.Net;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Vitarish.Intelligence.Contracts;
using Vitarish.Modules.Aggregator.Client;

namespace Vitarish.Intelligence.Client;

public sealed class IntelligenceClient(HttpClient httpClient) : AuthenticatedModuleApiClient(httpClient), IIntelligenceClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<IntelligenceResult<ResponderResult>> RespondAsync(ResponderRequest request, CancellationToken cancellationToken) =>
        Map(await PostJsonAsync<ResponderRequest, ResponderResult>(IntelligenceApiRoutes.Base + IntelligenceApiRoutes.Responder, request, cancellationToken).ConfigureAwait(false));

    public IAsyncEnumerable<IntelligenceStreamChunk<ResponderProgress>> StreamResponderAsync(ResponderRequest request, CancellationToken cancellationToken) =>
        StreamAsync<ResponderRequest, ResponderProgress>(IntelligenceApiRoutes.Base + IntelligenceApiRoutes.ResponderStream, request, cancellationToken);

    public async Task<IntelligenceResult<TranscriptionResult>> TranscribeAsync(TranscriptionRequest request, CancellationToken cancellationToken) =>
        Map(await PostJsonAsync<TranscriptionRequest, TranscriptionResult>(IntelligenceApiRoutes.Base + IntelligenceApiRoutes.Transcription, request, cancellationToken).ConfigureAwait(false));

    public IAsyncEnumerable<IntelligenceStreamChunk<TranscriptionProgress>> StreamTranscriptionAsync(TranscriptionRequest request, CancellationToken cancellationToken) =>
        StreamAsync<TranscriptionRequest, TranscriptionProgress>(IntelligenceApiRoutes.Base + IntelligenceApiRoutes.TranscriptionStream, request, cancellationToken);

    public async Task<IntelligenceResult<RewriteResult>> RewriteAsync(RewriteRequest request, CancellationToken cancellationToken) =>
        Map(await PostJsonAsync<RewriteRequest, RewriteResult>(IntelligenceApiRoutes.Base + IntelligenceApiRoutes.Rewrite, request, cancellationToken).ConfigureAwait(false));

    public IAsyncEnumerable<IntelligenceStreamChunk<RewriteProgress>> StreamRewriteAsync(RewriteRequest request, CancellationToken cancellationToken) =>
        StreamAsync<RewriteRequest, RewriteProgress>(IntelligenceApiRoutes.Base + IntelligenceApiRoutes.RewriteStream, request, cancellationToken);

    private async IAsyncEnumerable<IntelligenceStreamChunk<TProgress>> StreamAsync<TRequest, TProgress>(string route, TRequest request, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using HttpRequestMessage httpRequest = new(HttpMethod.Post, route) { Content = JsonContent.Create(request, options: JsonOptions) };
        ModuleApiClientResult<HttpResponseMessage> sent = await SendRawAsync(httpRequest, cancellationToken).ConfigureAwait(false);
        if (!sent.IsSuccess) { yield return Failure<TProgress>(MapFailure(sent.Failure!)); yield break; }
        using HttpResponseMessage response = sent.Value!;
        if (!response.IsSuccessStatusCode) { yield return Failure<TProgress>(MapFailure(await CreateFailureAsync(response, cancellationToken).ConfigureAwait(false))); yield break; }
        await using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using StreamReader reader = new(stream);
        long sequence = 0;
        while (true)
        {
            string? line;
            IntelligenceError? readError = null;
            try { line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false); }
            catch (OperationCanceledException) { line = null; readError = new IntelligenceError(IntelligenceErrorCode.Cancellation, "The stream was canceled."); }
            catch (Exception ex) { line = null; readError = new IntelligenceError(IntelligenceErrorCode.UnexpectedFailure, ex.Message); }
            if (readError is not null) { yield return Failure<TProgress>(readError); yield break; }
            if (line is null) { break; }
            if (string.IsNullOrWhiteSpace(line)) continue;
            IntelligenceStreamChunk<TProgress>? chunk = null;
            IntelligenceError? parseError = null;
            try { chunk = JsonSerializer.Deserialize<IntelligenceStreamChunk<TProgress>>(line, JsonOptions); }
            catch (JsonException ex) { parseError = new IntelligenceError(IntelligenceErrorCode.DeserializationFailure, ex.Message); }
            if (parseError is not null) { yield return Failure<TProgress>(parseError); yield break; }
            if (chunk is null) { yield return Failure<TProgress>(new IntelligenceError(IntelligenceErrorCode.MalformedChunk, "The stream emitted an empty or malformed chunk.")); yield break; }
            if (chunk.Metadata.Sequence < sequence) { yield return Failure<TProgress>(new IntelligenceError(IntelligenceErrorCode.MalformedChunk, "The stream emitted an out-of-order chunk.")); yield break; }
            sequence = chunk.Metadata.Sequence;
            yield return chunk;
            if (chunk.Kind is StreamChunkKind.Completed or StreamChunkKind.Failed or StreamChunkKind.Canceled) yield break;
        }
        yield return Failure<TProgress>(new IntelligenceError(IntelligenceErrorCode.PartialStreamFailure, "The stream ended without a terminal chunk."));
    }

    protected override ModuleApiClientFailure CreateFailure(HttpStatusCode statusCode, string message, int? statusCodeValue) => new(statusCode.ToString(), message, statusCodeValue);
    private static IntelligenceResult<T> Map<T>(ModuleApiClientResult<T> result) => result.IsSuccess ? IntelligenceResult<T>.Success(result.Value!) : IntelligenceResult<T>.Failed(MapFailure(result.Failure!));
    private static IntelligenceError MapFailure(ModuleApiClientFailure failure) => new(MapCode(failure), failure.Message, failure.StatusCode);
    private static IntelligenceErrorCode MapCode(ModuleApiClientFailure failure) => failure.Code switch { "Canceled" => IntelligenceErrorCode.Cancellation, "Timeout" => IntelligenceErrorCode.ProviderTimeout, "Transport" => IntelligenceErrorCode.ProviderUnavailable, "Deserialization" => IntelligenceErrorCode.DeserializationFailure, nameof(HttpStatusCode.BadRequest) => IntelligenceErrorCode.ValidationFailure, nameof(HttpStatusCode.Unauthorized) => IntelligenceErrorCode.Unauthorized, nameof(HttpStatusCode.Forbidden) => IntelligenceErrorCode.Forbidden, nameof(HttpStatusCode.UnsupportedMediaType) => IntelligenceErrorCode.UnsupportedMediaType, nameof(HttpStatusCode.RequestEntityTooLarge) => IntelligenceErrorCode.TooLargePayload, nameof(HttpStatusCode.NotFound) => IntelligenceErrorCode.FileUnavailable, nameof(HttpStatusCode.GatewayTimeout) => IntelligenceErrorCode.ProviderTimeout, nameof(HttpStatusCode.ServiceUnavailable) => IntelligenceErrorCode.ProviderUnavailable, _ => IntelligenceErrorCode.UnexpectedFailure };
    private static IntelligenceStreamChunk<T> Failure<T>(IntelligenceError error) => new(StreamChunkKind.Failed, new IntelligenceStreamMetadata(Guid.NewGuid().ToString("N"), 0, DateTimeOffset.UtcNow, IntelligenceProgressStage.Failed), default, error);
}
