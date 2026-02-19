using Microsoft.Extensions.Options;
using Vitarish.Intelligence.Application.Abstractions;
using Vitarish.Intelligence.Domain.Models;
using Vitarish.Intelligence.Domain.Options;
using Vitarish.Intelligence.Infrastructure.Services.Google;
using Vitarish.Intelligence.Infrastructure.Services.Ollama;
using Vitarish.Intelligence.Infrastructure.Services.OpenAi;

namespace Vitarish.Intelligence.Infrastructure.Services.Dispatching;

public sealed class ProviderDispatchingResponder : IResponder
{
    private readonly IOptionsMonitor<AiOptions> _aiOptions;
    private readonly ModelRegistry _modelRegistry;
    private readonly Lazy<OpenAiResponder> _openAiResponder;
    private readonly Lazy<OllamaResponder> _ollamaResponder;
    private readonly Lazy<GoogleResponder> _googleResponder;

    public ProviderDispatchingResponder(IOptionsMonitor<AiOptions> aiOptions, ModelRegistry modelRegistry)
    {
        _aiOptions = aiOptions;
        _modelRegistry = modelRegistry;
        _openAiResponder = new(() => new OpenAiResponder(_aiOptions));
        _ollamaResponder = new(() => new OllamaResponder(_aiOptions));
        _googleResponder = new(() => new GoogleResponder(_aiOptions));
    }

    private IResponder GetResponder(ResponderOptions options)
    {
        string? model = string.IsNullOrWhiteSpace(options.Model)
            ? _aiOptions.CurrentValue.DefaultResponseModel
            : options.Model.Trim();

        string? provider = ModelRegistry.GetProviderForModel(model);

        return provider switch
        {
            "Ollama" => _ollamaResponder.Value,
            "Google" => _googleResponder.Value,
            "OpenAI" => _openAiResponder.Value,
            _ => _openAiResponder.Value
        };
    }

    public Task<IReadOnlyList<ResponderOutput>> GenerateAsync(IReadOnlyList<ResponderInput> input, ResponderOptions options, CancellationToken ct = default) => GetResponder(options).GenerateAsync(input, options, ct);

    public IAsyncEnumerable<ResponderOutput> StreamAsync(IReadOnlyList<ResponderInput> input, ResponderOptions options, CancellationToken ct = default) => GetResponder(options).StreamAsync(input, options, ct);
}
