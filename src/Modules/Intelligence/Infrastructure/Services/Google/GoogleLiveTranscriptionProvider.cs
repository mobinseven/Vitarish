using global::Google.Api.Gax.Grpc;
using global::Google.Apis.Auth.OAuth2;
using global::Google.Cloud.Speech.V2;
using global::Google.Protobuf;
using global::Grpc.Core;
using Microsoft.Extensions.Options;
using System.Threading.Channels;
using Vitarish.Intelligence.Application.Abstractions;
using Vitarish.Intelligence.Application.LiveTranscription;
using Vitarish.Intelligence.Domain.Options;

namespace Vitarish.Intelligence.Infrastructure.Services.Google;

public sealed class GoogleLiveTranscriptionProvider : ITranscriptionProvider
{
    internal const int MaxProviderAudioBytes = 15_000;
    internal const string Model = "chirp_2";

    private readonly GoogleSpeechOptions _options;
    private readonly SemaphoreSlim _clientGate = new(1, 1);
    private SpeechClient? _client;

    public GoogleLiveTranscriptionProvider(IOptions<AiOptions> aiOptions)
    {
        ArgumentNullException.ThrowIfNull(aiOptions);
        _options = aiOptions.Value.Google.Speech ?? new GoogleSpeechOptions();
    }

    public bool IsAvailable => HasUsableConfiguration(_options);

    public async Task TranscribeAsync(
        ChannelReader<byte[]> audio,
        LiveTranscriptionProviderRequest request,
        Func<TranscriptionProviderEvent, CancellationToken, ValueTask> publish,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(audio);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(publish);

        try
        {
            SpeechClient client = await EnsureClientAsync(cancellationToken).ConfigureAwait(false);
            using CancellationTokenSource providerCancellation =
                CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            using SpeechClient.StreamingRecognizeStream call = client.StreamingRecognize(
                callSettings: CallSettings.FromCancellationToken(providerCancellation.Token));

            await call.WriteAsync(new StreamingRecognizeRequest
            {
                Recognizer = BuildRecognizerName(_options),
                StreamingConfig = CreateStreamingConfig(request.Language)
            }).ConfigureAwait(false);

            await publish(TranscriptionProviderEvent.Ready(), providerCancellation.Token)
                .ConfigureAwait(false);

            Task sendAudio = SendAudioAsync(call, audio, providerCancellation.Token);
            Task receiveResults = ReceiveResultsAsync(call, publish, providerCancellation.Token);
            try
            {
                await RunDuplexAsync(sendAudio, receiveResults, providerCancellation)
                    .ConfigureAwait(false);
            }
            finally
            {
                providerCancellation.Cancel();
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (RpcException exception) when (
            exception.StatusCode is StatusCode.Cancelled && cancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException(
                "The live transcription operation was canceled.",
                exception,
                cancellationToken);
        }
        catch (TranscriptionProviderException)
        {
            throw;
        }
        catch (RpcException exception)
        {
            throw CreateProviderException(exception.StatusCode, exception);
        }
        catch (IOException exception)
        {
            throw new TranscriptionProviderException(
                LiveTranscriptionErrorCode.ProviderUnavailable,
                "The live transcription provider is unavailable.",
                exception);
        }
        catch (UnauthorizedAccessException exception)
        {
            throw new TranscriptionProviderException(
                LiveTranscriptionErrorCode.ProviderUnavailable,
                "The live transcription provider configuration is unavailable.",
                exception);
        }
    }

    internal static StreamingRecognitionConfig CreateStreamingConfig(string language) => new()
    {
        Config = new RecognitionConfig
        {
            AutoDecodingConfig = new AutoDetectDecodingConfig(),
            LanguageCodes = { language },
            Model = Model
        }
    };

    internal static string BuildRecognizerName(GoogleSpeechOptions options) =>
        RecognizerName.FromProjectLocationRecognizer(
            options.ProjectId.Trim(),
            options.Location.Trim(),
            "_").ToString();

    internal static IEnumerable<ArraySegment<byte>> EnumerateProviderChunks(byte[] frame)
    {
        for (int offset = 0; offset < frame.Length; offset += MaxProviderAudioBytes)
        {
            yield return new ArraySegment<byte>(
                frame,
                offset,
                Math.Min(MaxProviderAudioBytes, frame.Length - offset));
        }
    }

    internal static bool HasUsableConfiguration(GoogleSpeechOptions options)
    {
        if (IsPlaceholder(options.ProjectId) || IsPlaceholder(options.Location))
        {
            return false;
        }

        return string.IsNullOrWhiteSpace(options.CredentialsPath)
            || File.Exists(options.CredentialsPath);
    }

    internal static TranscriptionProviderException CreateProviderException(
        StatusCode statusCode,
        Exception? innerException = null) => statusCode switch
        {
            StatusCode.DeadlineExceeded => new(
                LiveTranscriptionErrorCode.ProviderTimeout,
                "The live transcription provider timed out.",
                innerException),
            StatusCode.InvalidArgument or StatusCode.OutOfRange or StatusCode.DataLoss => new(
                LiveTranscriptionErrorCode.InvalidAudio,
                "The live transcription provider rejected the audio stream.",
                innerException),
            _ => new(
                LiveTranscriptionErrorCode.ProviderUnavailable,
                "The live transcription provider is unavailable.",
                innerException)
        };

    private async Task<SpeechClient> EnsureClientAsync(CancellationToken cancellationToken)
    {
        SpeechClient? current = Volatile.Read(ref _client);
        if (current is not null)
        {
            return current;
        }

        if (!IsAvailable)
        {
            throw new TranscriptionProviderException(
                LiveTranscriptionErrorCode.ProviderUnavailable,
                "The live transcription provider is not configured.");
        }

        await _clientGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            current = _client;
            if (current is not null)
            {
                return current;
            }

            try
            {
                SpeechClientBuilder builder = new()
                {
                    Endpoint = $"{_options.Location.Trim()}-speech.googleapis.com"
                };

                if (!string.IsNullOrWhiteSpace(_options.CredentialsPath))
                {
#pragma warning disable CS0618
                    builder.GoogleCredential = GoogleCredential.FromFile(_options.CredentialsPath);
#pragma warning restore CS0618
                }

                current = await builder.BuildAsync(cancellationToken).ConfigureAwait(false);
                Volatile.Write(ref _client, current);
                return current;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                throw new TranscriptionProviderException(
                    LiveTranscriptionErrorCode.ProviderUnavailable,
                    "The live transcription provider configuration is unavailable.",
                    exception);
            }
        }
        finally
        {
            _clientGate.Release();
        }
    }

    private static async Task SendAudioAsync(
        SpeechClient.StreamingRecognizeStream call,
        ChannelReader<byte[]> audio,
        CancellationToken cancellationToken)
    {
        await foreach (byte[] frame in audio.ReadAllAsync(cancellationToken).ConfigureAwait(false))
        {
            foreach (ArraySegment<byte> chunk in EnumerateProviderChunks(frame))
            {
                await call.WriteAsync(new StreamingRecognizeRequest
                {
                    Audio = ByteString.CopyFrom(chunk.Array!, chunk.Offset, chunk.Count)
                }).ConfigureAwait(false);
            }
        }

        await call.WriteCompleteAsync().ConfigureAwait(false);
    }

    private static async Task ReceiveResultsAsync(
        SpeechClient.StreamingRecognizeStream call,
        Func<TranscriptionProviderEvent, CancellationToken, ValueTask> publish,
        CancellationToken cancellationToken)
    {
        AsyncResponseStream<StreamingRecognizeResponse> responses = call.GetResponseStream();
        await foreach (StreamingRecognizeResponse response in
            responses.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            foreach (string text in EnumerateFinalSegments(response))
            {
                await publish(
                    TranscriptionProviderEvent.FinalSegment(text),
                    cancellationToken).ConfigureAwait(false);
            }
        }
    }

    internal static IEnumerable<string> EnumerateFinalSegments(
        StreamingRecognizeResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);

        foreach (StreamingRecognitionResult result in response.Results)
        {
            if (!result.IsFinal || result.Alternatives.Count == 0)
            {
                continue;
            }

            string text = result.Alternatives[0].Transcript.Trim();
            if (text.Length > 0)
            {
                yield return text;
            }
        }
    }

    private static async Task RunDuplexAsync(
        Task sendAudio,
        Task receiveResults,
        CancellationTokenSource providerCancellation)
    {
        Task first = await Task.WhenAny(sendAudio, receiveResults).ConfigureAwait(false);
        if (first == receiveResults && receiveResults.IsCompletedSuccessfully && !sendAudio.IsCompleted)
        {
            providerCancellation.Cancel();
            await ObserveCompletionAsync(sendAudio).ConfigureAwait(false);
            throw new TranscriptionProviderException(
                LiveTranscriptionErrorCode.ProviderUnavailable,
                "The live transcription provider ended before audio upload completed.");
        }

        if (!first.IsCompletedSuccessfully)
        {
            providerCancellation.Cancel();
            await ObserveCompletionAsync(first == sendAudio ? receiveResults : sendAudio)
                .ConfigureAwait(false);
            await first.ConfigureAwait(false);
        }

        await (first == sendAudio ? receiveResults : sendAudio).ConfigureAwait(false);
    }

    private static async Task ObserveCompletionAsync(Task task)
    {
        try
        {
            await task.ConfigureAwait(false);
        }
        catch (Exception)
        {
            // The first duplex failure is the authoritative provider outcome.
        }
    }

    private static bool IsPlaceholder(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        string normalized = value.Trim();
        return normalized.Equals("change-me", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("google-project-id", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains('<', StringComparison.Ordinal)
            || normalized.Contains('>', StringComparison.Ordinal)
            || normalized.Contains('{', StringComparison.Ordinal)
            || normalized.Contains('}', StringComparison.Ordinal);
    }
}
