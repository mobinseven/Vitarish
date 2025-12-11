using Microsoft.Extensions.Options;
using Vitarish.Intelligence.Domain.Models;
using Vitarish.Intelligence.Domain.Options;

namespace Vitarish.Intelligence.Infrastructure.Services.Dispatching;

public sealed class ModelRegistry
{
    private readonly IOptionsMonitor<AiOptions> _aiOptions;
    private AiOptions CurrentOptions => _aiOptions.CurrentValue;

    public ModelRegistry(IOptionsMonitor<AiOptions> aiOptions)
    {
        _aiOptions = aiOptions;
    }

    private static readonly IReadOnlyList<ModelInfo> AllTranscriberModels = [
        // Google models
        new() { Id = "long", DisplayName = "Google Long", Provider = "Google" },
        new() { Id = "short", DisplayName = "Google Short", Provider = "Google" },
        new() { Id = "chirp", DisplayName = "Google Chirp", Provider = "Google" },
        new() { Id = "chirp-v2", DisplayName = "Google Chirp v2", Provider = "Google" },
        new() { Id = "medical_dictation", DisplayName = "Google Medical Dictation", Provider = "Google" },
        // OpenAI models
        new() { Id = "gpt-4o-mini-transcribe", DisplayName = "GPT-4o Mini Transcribe", Provider = "OpenAI" },
        new() { Id = "gpt-4o-transcribe", DisplayName = "GPT-4o Transcribe", Provider = "OpenAI" },
        new() { Id = "gpt-4o-transcribe-diarize", DisplayName = "GPT-4o Transcribe Diarize", Provider = "OpenAI" },
        // Ollama models
        new() { Id = "whisper", DisplayName = "Whisper", Provider = "Ollama" },
        new() { Id = "whisper-small", DisplayName = "Whisper Small", Provider = "Ollama" },
        new() { Id = "whisper-medium", DisplayName = "Whisper Medium", Provider = "Ollama" },
        new() { Id = "whisper-large-v3", DisplayName = "Whisper Large v3", Provider = "Ollama" },
        new() { Id = "sense-voice-mini", DisplayName = "SenseVoice Mini", Provider = "Ollama" }
    ];

    private static readonly IReadOnlyList<ModelInfo> AllResponderModels = [
        // Google models
        new() { Id = "gemini-2.5-pro", DisplayName = "Gemini 2.5 Pro", Provider = "Google" },
        new() { Id = "gemini-2.5-flash", DisplayName = "Gemini 2.5 Flash", Provider = "Google" },
        new() { Id = "gemini-2.5-flash-lite", DisplayName = "Gemini 2.5 Flash Lite", Provider = "Google" },
        // OpenAI models
        new() { Id = "gpt-5-2025-08-07", DisplayName = "GPT-5", Provider = "OpenAI" },
        new() { Id = "gpt-5-mini-2025-08-07", DisplayName = "GPT-5 Mini", Provider = "OpenAI" },
        new() { Id = "gpt-5-nano-2025-08-07", DisplayName = "GPT-5 Nano", Provider = "OpenAI" },
        // Ollama models
        new() { Id = "qwen3:0.6b", DisplayName = "Qwen3 0.6B", Provider = "Ollama" },
        new() { Id = "qwen3:1.7b", DisplayName = "Qwen3 1.7B", Provider = "Ollama" },
        new() { Id = "qwen3:4b", DisplayName = "Qwen3 4B", Provider = "Ollama" }
    ];

    public AvailableModels GetAvailableModels()
    {
        List<ModelInfo> transcriberModels = FilterTranscriberModels();
        List<ModelInfo> responderModels = FilterResponderModels();

        return new AvailableModels
        {
            TranscriberModels = transcriberModels,
            ResponderModels = responderModels
        };
    }

    private static readonly IReadOnlyList<ModelInfo> AllModels = [.. AllResponderModels, .. AllTranscriberModels];

    public string? GetProviderForModel(string? modelId)
    {
        if(string.IsNullOrWhiteSpace(modelId))
        {
            return null;
        }

        ModelInfo? model = AllModels.FirstOrDefault(m => string.Equals(m.Id, modelId, StringComparison.OrdinalIgnoreCase));
        return model?.Provider;
    }

    private List<ModelInfo> FilterTranscriberModels()
    {
        List<ModelInfo> models = [];

        // Include OpenAI models if API key is configured
        if(!string.IsNullOrWhiteSpace(CurrentOptions.OpenAi.ApiKey))
        {
            models.AddRange(AllTranscriberModels.Where(m => m.Provider == "OpenAI"));
        }

        // Include Google models if configured
        if(IsGoogleSpeechConfigured())
        {
            models.AddRange(AllTranscriberModels.Where(m => m.Provider == "Google"));
        }

        // Include Ollama models if configured
        if(!string.IsNullOrWhiteSpace(CurrentOptions.Ollama.BaseUrl))
        {
            models.AddRange(AllTranscriberModels.Where(m => m.Provider == "Ollama"));
        }

        return models;
    }

    private List<ModelInfo> FilterResponderModels()
    {
        List<ModelInfo> models = [];

        // Include OpenAI models if API key is configured
        if(!string.IsNullOrWhiteSpace(CurrentOptions.OpenAi.ApiKey))
        {
            models.AddRange(AllResponderModels.Where(m => m.Provider == "OpenAI"));
        }

        // Include Ollama models if configured
        if(!string.IsNullOrWhiteSpace(CurrentOptions.Ollama.BaseUrl))
        {
            models.AddRange(AllResponderModels.Where(m => m.Provider == "Ollama"));
        }

        // Include Google models if configured
        if(IsGoogleGeminiConfigured())
        {
            models.AddRange(AllResponderModels.Where(m => m.Provider == "Google"));
        }

        return models;
    }

    private bool IsGoogleSpeechConfigured()
    {
        return !string.IsNullOrWhiteSpace(CurrentOptions.Google.Speech.ProjectId)
                && !string.IsNullOrWhiteSpace(CurrentOptions.Google.Speech.Location)
                && !string.IsNullOrWhiteSpace(CurrentOptions.Google.Speech.RecognizerId);
    }

    private bool IsGoogleGeminiConfigured()
    {
        return !string.IsNullOrWhiteSpace(CurrentOptions.Google.Gemini.ApiKey);
    }
}
