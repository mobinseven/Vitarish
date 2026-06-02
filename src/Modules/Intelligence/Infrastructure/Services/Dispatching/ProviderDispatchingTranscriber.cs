using Microsoft.Extensions.Options;
using System.Threading.Channels;
using Vitarish.Intelligence.Application.Abstractions;
using Vitarish.Intelligence.Domain.Options;
using Vitarish.Intelligence.Infrastructure.Services.Google;
using Vitarish.Intelligence.Infrastructure.Services.Ollama;
using Vitarish.Intelligence.Infrastructure.Services.OpenAi;

namespace Vitarish.Intelligence.Infrastructure.Services.Dispatching;

public sealed class ProviderDispatchingTranscriber : ITranscriber
{
    private readonly IOptionsMonitor<AiOptions> _aiOptions;
    private readonly ModelRegistry _modelRegistry;
    private readonly Lazy<OpenAiTranscriber> _openAiTranscriber;
    private readonly Lazy<OllamaTranscriber> _ollamaTranscriber;
    private readonly Lazy<GoogleTranscriber> _googleTranscriber;

    public ProviderDispatchingTranscriber(IOptionsMonitor<AiOptions> aiOptions, ModelRegistry modelRegistry)
    {
        _aiOptions = aiOptions;
        _modelRegistry = modelRegistry;
        _openAiTranscriber = new(() => new OpenAiTranscriber(_aiOptions));
        _ollamaTranscriber = new(() => new OllamaTranscriber(_aiOptions));
        _googleTranscriber = new(() => new GoogleTranscriber(_aiOptions));
    }

    public Task<string?> TranscribeAsync(Stream audio, string mimeType, string? language, string? modelId, CancellationToken cancellationToken)
    {
        GetModelAndTranscriber(modelId, out string model, out ITranscriber transcriber);
        return transcriber.TranscribeAsync(audio, mimeType, language, model, cancellationToken);
    }

    public IAsyncEnumerable<string> StreamTranscribeAsync(
        ChannelReader<byte[]> audio,
        string mimeType,
        string? language,
        string? modelId,
        CancellationToken cancellationToken)
    {
        GetModelAndTranscriber(modelId, out string model, out ITranscriber transcriber);
        return transcriber.StreamTranscribeAsync(audio, mimeType, language, model, cancellationToken);
    }

    private void GetModelAndTranscriber(string? modelId, out string model, out ITranscriber transcriber)
    {
        model = ResolveModel(modelId);
        string? provider = ModelRegistry.GetProviderForModel(model)?.ToLower();
        transcriber = provider switch
        {
            "ollama" => _ollamaTranscriber.Value,
            "google" => _googleTranscriber.Value,
            "openai" => _openAiTranscriber.Value,
            _ => throw new InvalidOperationException($"Model {modelId} is not supported")
        };
    }

    private string ResolveModel(string? requestedModel) => string.IsNullOrWhiteSpace(requestedModel)
                ? _aiOptions.CurrentValue.DefaultTranscriptionModel
                : requestedModel.Trim();
}
