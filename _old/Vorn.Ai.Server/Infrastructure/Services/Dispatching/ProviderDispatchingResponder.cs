using Vorn.Ai.Server.Domain.Models;
using Vorn.Ai.Server.Domain.Options;
using Vorn.Ai.Server.Infrastructure.Interfaces;
using Vorn.Ai.Server.Infrastructure.Services.Google;
using Vorn.Ai.Server.Infrastructure.Services.Ollama;
using Vorn.Ai.Server.Infrastructure.Services.OpenAi;

namespace Vorn.Ai.Server.Infrastructure.Services.Dispatching;

internal sealed class ProviderDispatchingResponder(VornOptions vornOptions, ModelRegistry modelRegistry) : IResponder
{
    private readonly Lazy<OpenAiResponder> _openAiResponder = new(() => new OpenAiResponder(vornOptions));
    private readonly Lazy<OllamaResponder> _ollamaResponder = new(() => new OllamaResponder(vornOptions));
    private readonly Lazy<GoogleResponder> _googleResponder = new(() => new GoogleResponder(vornOptions));

    private IResponder GetResponder(ResponderOptions options)
    {
        string? model = string.IsNullOrWhiteSpace(options.Model)
            ? vornOptions.Ai.DefaultResponseModel
            : options.Model.Trim();

        string? provider = modelRegistry.GetProviderForModel(model);

        return provider switch
        {
            "Ollama" => _ollamaResponder.Value,
            "Google" => _googleResponder.Value,
            "OpenAI" => _openAiResponder.Value,
            _ => _openAiResponder.Value
        };
    }

    public Task<IReadOnlyList<ResponderOutput>> GenerateAsync(IReadOnlyList<ResponderInput> input, ResponderOptions options, CancellationToken ct = default)
    {
        return GetResponder(options).GenerateAsync(input, options, ct);
    }

    public IAsyncEnumerable<ResponderOutput> StreamAsync(IReadOnlyList<ResponderInput> input, ResponderOptions options, CancellationToken ct = default)
    {
        return GetResponder(options).StreamAsync(input, options, ct);
    }
}
