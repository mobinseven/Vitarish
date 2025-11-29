namespace Vorn.Ai.Server.Infrastructure.Services.OpenAi;

using OpenAI;
using OpenAI.Audio;
using System.IO;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using Vorn.Ai.Server.Domain.Options;

public interface IAudioTranscriber
{
    IAsyncEnumerable<string> StreamTranscribeAsync(ChannelReader<byte[]> audio, string mimeType, string? language, CancellationToken cancellationToken);
    Task<string?> TranscribeAsync(Stream audio, string mimeType, string? language, CancellationToken cancellationToken);
}

public sealed class AudioTranscriber(VornOptions vornOptions) : IAudioTranscriber
{
    private AudioClient audioClient = default!;

    private void EnsureClient()
        => audioClient ??= new OpenAIClient(vornOptions.Ai.ApiKey).GetAudioClient(vornOptions.Ai.TranscriptionModel);

    public async Task<string?> TranscribeAsync(Stream audio, string mimeType, string? language, CancellationToken cancellationToken)
    {
        EnsureClient();

        AudioTranscriptionOptions options = new()
        {
            ResponseFormat = AudioTranscriptionFormat.Simple,
            Language = language ?? "fa"
        };

        AudioTranscription transcription =
            await audioClient.TranscribeAudioAsync(audio, $"audio.{GetExtension(mimeType)}", options, cancellationToken); // OpenAI detects format from file extension

        return transcription.Text;
    }
#pragma warning disable OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates.

    public async IAsyncEnumerable<string> StreamTranscribeAsync(
        ChannelReader<byte[]> audio,                 // ChannelReader instead of IAsyncEnumerable
        string mimeType,
        string? language,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        EnsureClient(); // initializes your audioClient

        var options = new AudioTranscriptionOptions
        {
            ResponseFormat = AudioTranscriptionFormat.Simple, // plain text
            Includes = AudioTranscriptionIncludes.Logprobs,
            Language = language ?? "fa"
        };

        string fileName = $"audio.{GetExtension(mimeType)}";

        // Pipe bridges channel -> Stream for the SDK
        var pipe = new Pipe();

        // We’ll be able to stop the pump when we receive a *done* update.
        using var stopPumpCts = new CancellationTokenSource();
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken /* server->client unsubscribe */,
            stopPumpCts.Token);

        // Pump: ChannelReader<byte[]> -> Pipe.Writer
        var pump = Task.Run(async () =>
        {
            try
            {
                await foreach (var chunk in audio.ReadAllAsync(linkedCts.Token))
                {
                    if (chunk is { Length: > 0 })
                        await pipe.Writer.WriteAsync(chunk, linkedCts.Token);
                }
            }
            catch (OperationCanceledException)
            {
                // swallow – completion handled below
            }
            finally
            {
                await pipe.Writer.CompleteAsync();
            }
        });

        await using var stream = pipe.Reader.AsStream(leaveOpen: false);

        // Start streaming transcription from the SDK
        var updates = audioClient.TranscribeAudioStreamingAsync(
            stream, fileName, options, CancellationToken.None);

        // Stream text back to the caller
        await foreach (var update in updates.WithCancellation(cancellationToken))
        {
            if (update is StreamingAudioTranscriptionTextDeltaUpdate textDelta)
            {
                var piece = textDelta.Delta;
                if (!string.IsNullOrEmpty(piece))
                    yield return piece;
            }
            else if (update is StreamingAudioTranscriptionTextDoneUpdate)
            {
                // Tell the pump to stop accepting more audio; this lets the SDK flush & close.
                stopPumpCts.Cancel();
                break;
            }
        }

        // Ensure the pump has finished and the pipe is closed
        await pump.ConfigureAwait(false);
    }

#pragma warning restore OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates.

    private static string GetExtension(string mimeType)
        => mimeType switch
        {
            "audio/webm" => "webm",
            "audio/ogg" => "ogg",
            "audio/mpeg" => "mp3",
            "video/mp4" => "mp4",
            _ => "webm"
        };
}
