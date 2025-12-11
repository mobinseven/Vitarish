
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Audio;
using System.Collections.Concurrent;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Vitarish.Intelligence.Application.Abstractions;
using Vitarish.Intelligence.Domain.Options;

namespace Vitarish.Intelligence.Infrastructure.Services.OpenAi;

public sealed class OpenAiTranscriber(IOptionsMonitor<AiOptions> aiOptions) : ITranscriber
{
    private readonly ConcurrentDictionary<string, AudioClient> clients = new(StringComparer.OrdinalIgnoreCase);
    private OpenAIClient? client;

    public async Task<string?> TranscribeAsync(Stream audio, string mimeType, string? language, string? modelId, CancellationToken cancellationToken)
    {
        AudioClient audioClient = EnsureClient(modelId);
        AudioTranscriptionOptions options = new()
        {
            ResponseFormat = AudioTranscriptionFormat.Simple,
            Language = language ?? "fa"
        };

        AudioTranscription transcription = await audioClient.TranscribeAudioAsync(
            audio,
            $"audio.{GetExtension(mimeType)}",
            options,
            cancellationToken).ConfigureAwait(false);

        return transcription.Text;
    }

#pragma warning disable OPENAI001
    public async IAsyncEnumerable<string> StreamTranscribeAsync(
        ChannelReader<byte[]> audio,
        string mimeType,
        string? language,
        string? modelId,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        AudioClient audioClient = EnsureClient(modelId);
        AudioTranscriptionOptions options = new()
        {
            ResponseFormat = AudioTranscriptionFormat.Simple,
            Includes = AudioTranscriptionIncludes.Logprobs,
            Language = language ?? "fa"
        };

        string fileName = $"audio.{GetExtension(mimeType)}";
        Pipe pipe = new();
        using CancellationTokenSource stopPumpCts = new();
        using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, stopPumpCts.Token);

        Task pump = Task.Run(async () =>
        {
            try
            {
                await foreach (byte[] chunk in audio.ReadAllAsync(linkedCts.Token).ConfigureAwait(false))
                {
                    if (chunk is { Length: > 0 })
                    {
                        _ = await pipe.Writer.WriteAsync(chunk, linkedCts.Token).ConfigureAwait(false);
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                await pipe.Writer.CompleteAsync().ConfigureAwait(false);
            }
        }, CancellationToken.None);

        await using Stream stream = pipe.Reader.AsStream(leaveOpen: false);
        IAsyncEnumerable<StreamingAudioTranscriptionUpdate> updates = audioClient.TranscribeAudioStreamingAsync(stream, fileName, options, CancellationToken.None);

        await foreach(StreamingAudioTranscriptionUpdate update in updates.WithCancellation(cancellationToken))
        {
            if(update is StreamingAudioTranscriptionTextDeltaUpdate textDelta)
            {
                if(!string.IsNullOrEmpty(textDelta.Delta))
                {
                    yield return textDelta.Delta;
                }
            }
            else if(update is StreamingAudioTranscriptionTextDoneUpdate)
            {
                stopPumpCts.Cancel();
                break;
            }
        }

        await pump.ConfigureAwait(false);
    }
#pragma warning restore OPENAI001

    private AudioClient EnsureClient(string? modelId)
    {
        string resolvedModel = string.IsNullOrWhiteSpace(modelId)
            ? aiOptions.CurrentValue.DefaultTranscriptionModel
            : modelId;

        return clients.GetOrAdd(resolvedModel, CreateClient);
    }

    private AudioClient CreateClient(string modelId)
    {
        client ??= new OpenAIClient(aiOptions.CurrentValue.OpenAi.ApiKey);
        return client.GetAudioClient(modelId);
    }

    private static string GetExtension(string mimeType)
    {
        return mimeType switch
        {
            "audio/webm" => "webm",
            "audio/ogg" => "ogg",
            "audio/mpeg" => "mp3",
            "video/mp4" => "mp4",
            _ => "webm"
        };
    }
}
