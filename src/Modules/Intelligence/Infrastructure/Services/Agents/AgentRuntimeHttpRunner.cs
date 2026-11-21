using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vitarish.Intelligence.Application.Agents;
using Vitarish.Intelligence.Domain.Options;

namespace Vitarish.Intelligence.Infrastructure.Services.Agents;

public sealed class AgentRuntimeHttpRunner(
    HttpClient httpClient,
    IOptions<AgentRuntimeOptions> options,
    IAgentRuntimeCredentialService credentialService,
    ILogger<AgentRuntimeHttpRunner> logger) : IAgentRunner
{
    internal const string RunPath = "/api/internal/intelligence-agent/run";
    internal const string StreamPath = RunPath + "/stream";

    /// <summary>The largest accepted runtime stream line; the terminal result frame carries the conversation history.</summary>
    internal const int MaxFrameBytes = 8 * 1024 * 1024;

    /// <summary>The largest accepted stage frame, which carries a stage name and an ordinal only.</summary>
    internal const int MaxStageFrameBytes = 1024;

    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);

    public async Task<AgentRunResult> RunAsync(
        AgentDefinition definition,
        AgentInput input,
        IReadOnlyCollection<IAgentTool> tools,
        AgentExecutionContext context,
        AgentExecutionLimits limits,
        CancellationToken cancellationToken = default)
    {
        if (!TryCreateRequest(definition, input, tools, context, limits, out AgentRuntimeRunRequest? request, out AgentRunResult? unconfigured))
            return unconfigured;

        try
        {
            Stopwatch latency = Stopwatch.StartNew();
            using HttpRequestMessage httpRequest = CreateHttpRequest(RunPath, request);
            using HttpResponseMessage response = await httpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("The agent runtime returned HTTP {StatusCode}.", (int)response.StatusCode);
                return Failure(AgentRunErrorCode.ProviderUnavailable, "The agent runtime could not complete the request.");
            }

            AgentRuntimeRunResponse? result = await response.Content.ReadFromJsonAsync<AgentRuntimeRunResponse>(cancellationToken).ConfigureAwait(false);
            // An unreadable runtime response says nothing about the history and may follow executed
            // tools, so it is a provider failure, never a history rejection that could be retried.
            if (result is null)
                return Failure(AgentRunErrorCode.ProviderFailure, "The agent runtime returned an empty response.");

            return ToRunResult(result, latency.Elapsed);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Failure(AgentRunErrorCode.Cancelled, "The agent execution was cancelled.");
        }
        catch (JsonException)
        {
            return Failure(AgentRunErrorCode.ProviderFailure, "The agent runtime returned an invalid response.");
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or InvalidOperationException)
        {
            logger.LogError("The agent runtime request failed. Failure type: {FailureType}.", exception.GetType().Name);
            return Failure(AgentRunErrorCode.ProviderUnavailable, "The agent runtime could not be reached.");
        }
    }

    /// <summary>
    /// Runs one attempt over the runtime's NDJSON stream: stage frames followed by one terminal result
    /// frame. The read is bounded by a whole-turn deadline and per-frame size limits, awaits each stage
    /// report before reading on, and is never retried or replayed. A stream that ends without its
    /// terminal frame is an outcome-unknown failure, not a history rejection.
    /// </summary>
    public async Task<AgentRunResult> RunStreamedAsync(
        AgentDefinition definition,
        AgentInput input,
        IReadOnlyCollection<IAgentTool> tools,
        AgentExecutionContext context,
        AgentExecutionLimits limits,
        AgentAnswerStream? answerStream,
        IAgentRunProgress progress,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(progress);
        // The runtime streams answer text only for a run without tools, so the declaration is never sent with tools.
        AgentAnswerStream? declared = tools.Count == 0 ? answerStream : null;
        if (!TryCreateRequest(definition, input, tools, context, limits, out AgentRuntimeRunRequest? request, out AgentRunResult? unconfigured, declared))
            return unconfigured;

        using CancellationTokenSource turn = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        turn.CancelAfter(limits.EffectiveTimeout);
        bool headersReceived = false;
        try
        {
            Stopwatch latency = Stopwatch.StartNew();
            using HttpRequestMessage httpRequest = CreateHttpRequest(StreamPath, request);
            using HttpResponseMessage response = await httpClient
                .SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, turn.Token)
                .ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("The agent runtime stream returned HTTP {StatusCode}.", (int)response.StatusCode);
                return Failure(AgentRunErrorCode.ProviderUnavailable, "The agent runtime could not complete the request.");
            }

            headersReceived = true;
            await using Stream body = await response.Content.ReadAsStreamAsync(turn.Token).ConfigureAwait(false);
            AgentRuntimeRunResponse? result = null;
            int answerChars = 0;
            await foreach (ReadOnlyMemory<byte> line in ReadFramesAsync(body, turn.Token).ConfigureAwait(false))
            {
                if (result is not null)
                    return Failure(AgentRunErrorCode.ProviderFailure, "The agent runtime returned an invalid response.");
                AgentRuntimeStreamFrame frame = AgentRuntimeStreamFrame.Parse(line.Span, declared);
                if (frame.Stage is { } stage)
                    await progress.StageAsync(stage, turn.Token).ConfigureAwait(false);
                else if (frame.DeltaText is { } text)
                {
                    answerChars += text.Length;
                    if (answerChars > AgentRuntimeStreamFrame.MaxAnswerChars)
                        throw new InvalidDataException("The runtime streamed more answer text than allowed.");
                    await progress.AnswerDeltaAsync(declared!.MessageId, text, turn.Token).ConfigureAwait(false);
                }
                else
                    result = JsonSerializer.Deserialize<AgentRuntimeRunResponse>(line.Span, WebJson);
            }

            // No terminal frame means the outcome is unknown: tools may have run, so it is never replayed.
            return result is null
                ? Failure(AgentRunErrorCode.ProviderFailure, "The agent runtime stream ended without a final result.")
                : ToRunResult(result, latency.Elapsed);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Failure(AgentRunErrorCode.Cancelled, "The agent execution was cancelled.");
        }
        catch (OperationCanceledException) when (turn.IsCancellationRequested)
        {
            return Failure(AgentRunErrorCode.Timeout, "The agent execution timed out.");
        }
        catch (Exception exception) when (exception is JsonException or InvalidDataException)
        {
            return Failure(AgentRunErrorCode.ProviderFailure, "The agent runtime returned an invalid response.");
        }
        catch (Exception exception) when (headersReceived && exception is IOException or HttpRequestException)
        {
            logger.LogError("The agent runtime stream was interrupted. Failure type: {FailureType}.", exception.GetType().Name);
            return Failure(AgentRunErrorCode.ProviderFailure, "The agent runtime stream was interrupted.");
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or InvalidOperationException)
        {
            logger.LogError("The agent runtime request failed. Failure type: {FailureType}.", exception.GetType().Name);
            return Failure(AgentRunErrorCode.ProviderUnavailable, "The agent runtime could not be reached.");
        }
    }

    /// <summary>Yields each complete line, rejecting one that exceeds <see cref="MaxFrameBytes"/> or ends the stream unterminated.</summary>
    private static async IAsyncEnumerable<ReadOnlyMemory<byte>> ReadFramesAsync(
        Stream stream,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        byte[] buffer = new byte[16 * 1024];
        byte[] line = new byte[16 * 1024];
        int length = 0;
        while (true)
        {
            int read = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
            if (read == 0)
            {
                if (length > 0)
                    throw new InvalidDataException("The runtime stream ended inside a frame.");
                yield break;
            }

            int start = 0;
            while (start < read)
            {
                int newline = Array.IndexOf(buffer, (byte)'\n', start, read - start);
                int end = newline < 0 ? read : newline;
                int chunk = end - start;
                if (length + chunk > MaxFrameBytes)
                    throw new InvalidDataException("The runtime stream frame exceeds the size limit.");
                if (length + chunk > line.Length)
                    Array.Resize(ref line, Math.Min(MaxFrameBytes, Math.Max(line.Length * 2, length + chunk)));
                Buffer.BlockCopy(buffer, start, line, length, chunk);
                length += chunk;
                if (newline < 0)
                    break;
                start = newline + 1;
                if (length > 0)
                    yield return line.AsMemory(0, length);
                length = 0;
            }
        }
    }

    private bool TryCreateRequest(
        AgentDefinition definition,
        AgentInput input,
        IReadOnlyCollection<IAgentTool> tools,
        AgentExecutionContext context,
        AgentExecutionLimits limits,
        [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out AgentRuntimeRunRequest? request,
        [System.Diagnostics.CodeAnalysis.NotNullWhen(false)] out AgentRunResult? failure,
        AgentAnswerStream? answerStream = null)
    {
        if (string.IsNullOrWhiteSpace(options.Value.ToolCallbackUrl) ||
            string.IsNullOrWhiteSpace(options.Value.BaseUrl) ||
            string.IsNullOrWhiteSpace(options.Value.SharedSecret))
        {
            request = null;
            failure = Failure(AgentRunErrorCode.ProviderUnavailable, "The agent runtime is not configured.");
            return false;
        }

        AgentRuntimeExecutionEnvelope envelope = AgentRuntimeExecutionEnvelope.FromContext(
            context,
            DateTimeOffset.UtcNow.AddMinutes(5));
        request = new(
            definition,
            input,
            tools.Select(tool => tool.Definition).ToArray(),
            envelope,
            limits,
            options.Value.ToolCallbackUrl,
            credentialService.Create(envelope),
            answerStream);
        failure = null;

        logger.LogInformation(
            "Forwarding agent runtime request {ExecutionId} with {ToolCount} supplied tools: {ToolNames}.",
            context.ExecutionId,
            request.Tools.Count,
            string.Join(", ", request.Tools.Select(tool => tool.Name).OrderBy(name => name, StringComparer.Ordinal)));
        return true;
    }

    private HttpRequestMessage CreateHttpRequest(string path, AgentRuntimeRunRequest request)
    {
        HttpRequestMessage httpRequest = new(HttpMethod.Post, path)
        {
            Content = JsonContent.Create(request)
        };
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.Value.SharedSecret);
        return httpRequest;
    }

    private static AgentRunResult ToRunResult(AgentRuntimeRunResponse result, TimeSpan latency) => new(
        result.IsSuccess,
        result.Output,
        result.StructuredOutput,
        result.Usage,
        latency,
        result.Iterations,
        result.ToolCalls,
        result.Error)
    {
        History = result.History,
        ToolExecutions = result.ToolExecutions ?? []
    };

    private static AgentRunResult Failure(AgentRunErrorCode code, string message) => new(
        false, null, null, new(0, 0), TimeSpan.Zero, 0, 0, new(code, message));
}
