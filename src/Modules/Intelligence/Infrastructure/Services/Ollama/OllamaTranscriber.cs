using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using Vitarish.Intelligence.Application.Abstractions;
using Vitarish.Intelligence.Domain.Options;

namespace Vitarish.Intelligence.Infrastructure.Services.Ollama;

public sealed class OllamaTranscriber(IOptionsMonitor<AiOptions> aiOptions) : ITranscriber
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerOptions.Default)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private HttpClient? client;

    public async Task<string?> TranscribeAsync(Stream audio, string mimeType, string? language, string? modelId, CancellationToken cancellationToken)
    {
        string resolvedModel = ResolveModel(modelId);
        string format = ResolveAudioFormat(mimeType);
        string prompt = BuildPrompt(language);

        using MemoryStream buffer = new();
        await audio.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
        buffer.Position = 0;
        string base64 = Convert.ToBase64String(buffer.ToArray());

        OllamaChatRequest request = new()
        {
            Model = resolvedModel,
            Stream = false,
            Messages =
            [
                new OllamaChatMessage
                {
                    Role = "user",
                    Content =
                    [
                        new OllamaMessageContent
                        {
                            Type = "input_text",
                            Text = prompt
                        },
                        new OllamaMessageContent
                        {
                            Type = "input_audio",
                            Audio = new OllamaMessageAudio
                            {
                                Format = format,
                                Data = base64
                            }
                        }
                    ]
                }
            ]
        };

        using HttpRequestMessage message = new(HttpMethod.Post, "api/chat")
        {
            Content = new StringContent(JsonSerializer.Serialize(request, SerializerOptions), Encoding.UTF8, "application/json")
        };

        HttpClient httpClient = EnsureClient();
        using HttpResponseMessage response = await httpClient.SendAsync(message, cancellationToken).ConfigureAwait(false);
        _ = response.EnsureSuccessStatusCode();

        await using Stream contentStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        OllamaChatResponse? chatResponse = await JsonSerializer.DeserializeAsync<OllamaChatResponse>(contentStream, SerializerOptions, cancellationToken).ConfigureAwait(false);

        return ExtractTranscript(chatResponse);
    }

    public async IAsyncEnumerable<string> StreamTranscribeAsync(
        ChannelReader<byte[]> audio,
        string mimeType,
        string? language,
        string? modelId,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using MemoryStream buffer = new();
        await foreach (byte[] chunk in audio.ReadAllAsync(cancellationToken).ConfigureAwait(false))
        {
            if (chunk.Length == 0)
            {
                continue;
            }

            await buffer.WriteAsync(chunk, cancellationToken).ConfigureAwait(false);
        }

        buffer.Position = 0;
        string? transcript = await TranscribeAsync(buffer, mimeType, language, modelId, cancellationToken).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(transcript))
        {
            yield return transcript;
        }
    }

    private HttpClient EnsureClient()
    {
        if (client is not null)
        {
            return client;
        }

        client = new HttpClient
        {
            BaseAddress = new Uri(aiOptions.CurrentValue.Ollama.BaseUrl, UriKind.Absolute)
        };

        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return client;
    }

    private string ResolveModel(string? requestedModel)
    {
        if (!string.IsNullOrWhiteSpace(requestedModel))
        {
            return requestedModel;
        }

        string configured = aiOptions.CurrentValue.Ollama.DefaultTranscriberModel;
        return string.IsNullOrWhiteSpace(configured) ? "whisper" : configured;
    }

    private static string ResolveAudioFormat(string mimeType) => mimeType switch
    {
        "audio/webm" => "webm",
        "audio/ogg" => "ogg",
        "audio/mpeg" => "mp3",
        "audio/wav" => "wav",
        "audio/x-wav" => "wav",
        "video/mp4" => "mp4",
        _ => "wav"
    };

    private static string BuildPrompt(string? language) => string.IsNullOrWhiteSpace(language)
                ? "Transcribe the supplied audio to text."
                : $"Transcribe the supplied audio to {language} text.";

    private static string? ExtractTranscript(OllamaChatResponse? response) => response?.Message?.Content?.FirstOrDefault(c => string.Equals(c.Type, "output_text", StringComparison.OrdinalIgnoreCase))?.Text;

    private sealed class OllamaChatRequest
    {
        public string Model { get; set; } = string.Empty;
        public bool Stream { get; set; }
        public List<OllamaChatMessage> Messages { get; set; } = [];
    }

    private sealed class OllamaChatMessage
    {
        public string Role { get; set; } = "user";
        public List<OllamaMessageContent> Content { get; set; } = [];
    }

    private sealed class OllamaMessageContent
    {
        public string Type { get; set; } = string.Empty;
        public string? Text { get; set; }
        public OllamaMessageAudio? Audio { get; set; }
    }

    private sealed class OllamaMessageAudio
    {
        public string Format { get; set; } = string.Empty;
        public string Data { get; set; } = string.Empty;
    }

    private sealed class OllamaChatResponse
    {
        public OllamaChatMessageResponse? Message { get; set; }
    }

    private sealed class OllamaChatMessageResponse
    {
        public List<OllamaMessageResponseContent>? Content { get; set; }
    }

    private sealed class OllamaMessageResponseContent
    {
        public string? Type { get; set; }
        public string? Text { get; set; }
    }
}
