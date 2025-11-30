using System.Threading.Channels;
using Vorn.Ai.Server.Domain.Options;
using Vorn.Ai.Server.Infrastructure.Interfaces;
using Vorn.Ai.Server.Infrastructure.Services.Google;
using Vorn.Ai.Server.Infrastructure.Services.Ollama;
using Vorn.Ai.Server.Infrastructure.Services.OpenAi;

namespace Vorn.Ai.Server.Infrastructure.Services.Dispatching;

internal sealed class ProviderDispatchingTranscriber(VornOptions vornOptions, ModelRegistry modelRegistry) : ITranscriber
{
    private readonly Lazy<OpenAiTranscriber> _openAiTranscriber = new(() => new OpenAiTranscriber(vornOptions));
    private readonly Lazy<OllamaTranscriber> _ollamaTranscriber = new(() => new OllamaTranscriber(vornOptions));
    private readonly Lazy<GoogleTranscriber> _googleTranscriber = new(() => new GoogleTranscriber(vornOptions));

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
        string? provider = modelRegistry.GetProviderForModel(model);
        transcriber = provider switch
        {
            "Ollama" => _ollamaTranscriber.Value,
            "Google" => _googleTranscriber.Value,
            "OpenAi" => _openAiTranscriber.Value,
            _ => throw new InvalidOperationException($"Model {modelId} is not supported")
        };
    }

    private string ResolveModel(string? requestedModel)
    {
        return string.IsNullOrWhiteSpace(requestedModel)
                ? vornOptions.Ai.DefaultTranscriptionModel
                : requestedModel.Trim();
    }
}
