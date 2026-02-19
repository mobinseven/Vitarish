using Vitarish.Intelligence.Infrastructure.Services.Dispatching;

namespace Intelligence.Tests;

/// <summary>
/// Unit tests for the <see cref="ModelRegistry"/> class.
/// </summary>
public sealed class ModelRegistryTests
{
    [Fact]
    public void GetProviderForModel_WithOpenAiResponseModel_ReturnsOpenAI()
    {
        // Arrange
        IOptionsMonitor<AiOptions> aiOptions = AiOptionsFactory.CreateWithOpenAi().ToMonitor();
        var registry = new ModelRegistry(aiOptions);

        // Act
        var provider = registry.GetProviderForModel("gpt-5-nano-2025-08-07");

        // Assert
        provider.Should().Be("OpenAI");
    }

    [Fact]
    public void GetProviderForModel_WithOpenAiTranscriptionModel_ReturnsOpenAI()
    {
        // Arrange
        IOptionsMonitor<AiOptions> aiOptions = AiOptionsFactory.CreateWithOpenAi().ToMonitor();
        var registry = new ModelRegistry(aiOptions);

        // Act
        var provider = registry.GetProviderForModel("gpt-4o-mini-transcribe");

        // Assert
        provider.Should().Be("OpenAI");
    }

    [Fact]
    public void GetProviderForModel_WithGoogleResponderModel_ReturnsGoogle()
    {
        // Arrange
        IOptionsMonitor<AiOptions> aiOptions = AiOptionsFactory.CreateWithGoogle().ToMonitor();
        var registry = new ModelRegistry(aiOptions);

        // Act
        var provider = registry.GetProviderForModel("gemini-2.5-pro");

        // Assert
        provider.Should().Be("Google");
    }

    [Fact]
    public void GetProviderForModel_WithGoogleTranscriberModel_ReturnsGoogle()
    {
        // Arrange
        IOptionsMonitor<AiOptions> aiOptions = AiOptionsFactory.CreateWithGoogle().ToMonitor();
        var registry = new ModelRegistry(aiOptions);

        // Act
        var provider = registry.GetProviderForModel("chirp");

        // Assert
        provider.Should().Be("Google");
    }

    [Fact]
    public void GetProviderForModel_WithOllamaResponderModel_ReturnsOllama()
    {
        // Arrange
        IOptionsMonitor<AiOptions> aiOptions = AiOptionsFactory.CreateWithOllama().ToMonitor();
        var registry = new ModelRegistry(aiOptions);

        // Act
        var provider = registry.GetProviderForModel("qwen3:4b");

        // Assert
        provider.Should().Be("Ollama");
    }

    [Fact]
    public void GetProviderForModel_WithOllamaTranscriberModel_ReturnsOllama()
    {
        // Arrange
        IOptionsMonitor<AiOptions> aiOptions = AiOptionsFactory.CreateWithOllama().ToMonitor();
        var registry = new ModelRegistry(aiOptions);

        // Act
        var provider = registry.GetProviderForModel("whisper");

        // Assert
        provider.Should().Be("Ollama");
    }

    [Fact]
    public void GetProviderForModel_WithUnknownModel_ReturnsNull()
    {
        // Arrange
        IOptionsMonitor<AiOptions> aiOptions = AiOptionsFactory.CreateWithOpenAi().ToMonitor();
        var registry = new ModelRegistry(aiOptions);

        // Act
        var provider = registry.GetProviderForModel("unknown-model");

        // Assert
        provider.Should().BeNull();
    }

    [Fact]
    public void GetProviderForModel_WithNullModelId_ReturnsNull()
    {
        // Arrange
        IOptionsMonitor<AiOptions> aiOptions = AiOptionsFactory.CreateWithOpenAi().ToMonitor();
        var registry = new ModelRegistry(aiOptions);

        // Act
        var provider = registry.GetProviderForModel(null);

        // Assert
        provider.Should().BeNull();
    }

    [Fact]
    public void GetProviderForModel_WithEmptyModelId_ReturnsNull()
    {
        // Arrange
        IOptionsMonitor<AiOptions> aiOptions = AiOptionsFactory.CreateWithOpenAi().ToMonitor();
        var registry = new ModelRegistry(aiOptions);

        // Act
        var provider = registry.GetProviderForModel("");

        // Assert
        provider.Should().BeNull();
    }

    [Fact]
    public void GetProviderForModel_WithWhitespaceModelId_ReturnsNull()
    {
        // Arrange
        IOptionsMonitor<AiOptions> aiOptions = AiOptionsFactory.CreateWithOpenAi().ToMonitor();
        var registry = new ModelRegistry(aiOptions);

        // Act
        var provider = registry.GetProviderForModel("   ");

        // Assert
        provider.Should().BeNull();
    }

    [Fact]
    public void GetProviderForModel_IsCaseInsensitive()
    {
        // Arrange
        IOptionsMonitor<AiOptions> aiOptions = AiOptionsFactory.CreateWithOpenAi().ToMonitor();
        var registry = new ModelRegistry(aiOptions);

        // Act
        var provider1 = registry.GetProviderForModel("GPT-5-NANO-2025-08-07");
        var provider2 = registry.GetProviderForModel("gpt-5-nano-2025-08-07");

        // Assert
        provider1.Should().Be("OpenAI");
        provider2.Should().Be("OpenAI");
    }

    [Fact]
    public void GetAvailableModels_WhenOpenAiConfigured_IncludesOpenAiModels()
    {
        // Arrange
        IOptionsMonitor<AiOptions> aiOptions = AiOptionsFactory.CreateWithOpenAi().ToMonitor();
        var registry = new ModelRegistry(aiOptions);

        // Act
        AvailableModels availableModels = registry.GetAvailableModels();

        // Assert
        availableModels.ResponderModels.Should().Contain(m => m.Provider == "OpenAI");
        availableModels.TranscriberModels.Should().Contain(m => m.Provider == "OpenAI");
    }

    [Fact]
    public void GetAvailableModels_WhenOpenAiNotConfigured_ExcludesOpenAiModels()
    {
        // Arrange
        IOptionsMonitor<AiOptions> aiOptions = Options.Create(new AiOptions
        {
            OpenAi = new OpenAiOptions { ApiKey = "" }
        }).ToMonitor();
        var registry = new ModelRegistry(aiOptions);

        // Act
        AvailableModels availableModels = registry.GetAvailableModels();

        // Assert
        availableModels.ResponderModels.Should().NotContain(m => m.Provider == "OpenAI");
        availableModels.TranscriberModels.Should().NotContain(m => m.Provider == "OpenAI");
    }

    [Fact]
    public void GetAvailableModels_WhenOllamaConfigured_IncludesOllamaModels()
    {
        // Arrange
        IOptionsMonitor<AiOptions> aiOptions = AiOptionsFactory.CreateWithOllama().ToMonitor();
        var registry = new ModelRegistry(aiOptions);

        // Act
        AvailableModels availableModels = registry.GetAvailableModels();

        // Assert
        availableModels.ResponderModels.Should().Contain(m => m.Provider == "Ollama");
        availableModels.TranscriberModels.Should().Contain(m => m.Provider == "Ollama");
    }

    [Fact]
    public void GetAvailableModels_WhenOllamaNotConfigured_ExcludesOllamaModels()
    {
        // Arrange
        IOptionsMonitor<AiOptions> aiOptions = Options.Create(new AiOptions
        {
            Ollama = new OllamaOptions { BaseUrl = "" }
        }).ToMonitor();
        var registry = new ModelRegistry(aiOptions);

        // Act
        AvailableModels availableModels = registry.GetAvailableModels();

        // Assert
        availableModels.ResponderModels.Should().NotContain(m => m.Provider == "Ollama");
        availableModels.TranscriberModels.Should().NotContain(m => m.Provider == "Ollama");
    }

    [Fact]
    public void GetAvailableModels_WhenGoogleSpeechConfigured_IncludesGoogleTranscriberModels()
    {
        // Arrange
        IOptionsMonitor<AiOptions> aiOptions = AiOptionsFactory.CreateWithGoogle().ToMonitor();
        var registry = new ModelRegistry(aiOptions);

        // Act
        AvailableModels availableModels = registry.GetAvailableModels();

        // Assert
        availableModels.TranscriberModels.Should().Contain(m => m.Provider == "Google");
    }

    [Fact]
    public void GetAvailableModels_WhenGoogleGeminiConfigured_IncludesGoogleResponderModels()
    {
        // Arrange
        IOptionsMonitor<AiOptions> aiOptions = AiOptionsFactory.CreateWithGoogle().ToMonitor();
        var registry = new ModelRegistry(aiOptions);

        // Act
        AvailableModels availableModels = registry.GetAvailableModels();

        // Assert
        availableModels.ResponderModels.Should().Contain(m => m.Provider == "Google");
    }

    [Fact]
    public void GetAvailableModels_WhenGoogleNotConfigured_ExcludesGoogleModels()
    {
        // Arrange
        IOptionsMonitor<AiOptions> aiOptions = Options.Create(new AiOptions
        {
            Google = new GoogleOptions
            {
                Gemini = new GoogleGeminiOptions { ApiKey = "" },
                Speech = new GoogleSpeechOptions { ProjectId = "" }
            }
        }).ToMonitor();
        var registry = new ModelRegistry(aiOptions);

        // Act
        AvailableModels availableModels = registry.GetAvailableModels();

        // Assert
        availableModels.ResponderModels.Should().NotContain(m => m.Provider == "Google");
        availableModels.TranscriberModels.Should().NotContain(m => m.Provider == "Google");
    }

    [Fact]
    public void GetAvailableModels_WhenAllProvidersConfigured_IncludesAllModels()
    {
        // Arrange
        IOptionsMonitor<AiOptions> aiOptions = AiOptionsFactory.CreateWithAllProviders().ToMonitor();
        var registry = new ModelRegistry(aiOptions);

        // Act
        AvailableModels availableModels = registry.GetAvailableModels();

        // Assert
        availableModels.ResponderModels.Should().Contain(m => m.Provider == "OpenAI");
        availableModels.ResponderModels.Should().Contain(m => m.Provider == "Google");
        availableModels.ResponderModels.Should().Contain(m => m.Provider == "Ollama");
        availableModels.TranscriberModels.Should().Contain(m => m.Provider == "OpenAI");
        availableModels.TranscriberModels.Should().Contain(m => m.Provider == "Google");
        availableModels.TranscriberModels.Should().Contain(m => m.Provider == "Ollama");
    }

    [Fact]
    public void GetAvailableModels_WhenNoProvidersConfigured_ReturnsEmptyLists()
    {
        // Arrange
        IOptionsMonitor<AiOptions> aiOptions = Options.Create(new AiOptions
        {
            OpenAi = new OpenAiOptions { ApiKey = "" },
            Ollama = new OllamaOptions { BaseUrl = "" },
            Google = new GoogleOptions
            {
                Gemini = new GoogleGeminiOptions { ApiKey = "" },
                Speech = new GoogleSpeechOptions { ProjectId = "" }
            }
        }).ToMonitor();
        var registry = new ModelRegistry(aiOptions);

        // Act
        AvailableModels availableModels = registry.GetAvailableModels();

        // Assert
        availableModels.ResponderModels.Should().BeEmpty();
        availableModels.TranscriberModels.Should().BeEmpty();
    }

    [Theory]
    [InlineData("gpt-5-2025-08-07", "OpenAI")]
    [InlineData("gpt-5-mini-2025-08-07", "OpenAI")]
    [InlineData("gpt-5-nano-2025-08-07", "OpenAI")]
    [InlineData("gemini-2.5-pro", "Google")]
    [InlineData("gemini-2.5-flash", "Google")]
    [InlineData("gemini-2.5-flash-lite", "Google")]
    [InlineData("qwen3:0.6b", "Ollama")]
    [InlineData("qwen3:1.7b", "Ollama")]
    [InlineData("qwen3:4b", "Ollama")]
    public void GetProviderForModel_ResponderModels_ReturnCorrectProvider(string modelId, string expectedProvider)
    {
        // Arrange
        IOptionsMonitor<AiOptions> aiOptions = AiOptionsFactory.CreateWithAllProviders().ToMonitor();
        var registry = new ModelRegistry(aiOptions);

        // Act
        var provider = registry.GetProviderForModel(modelId);

        // Assert
        provider.Should().Be(expectedProvider);
    }

    [Theory]
    [InlineData("long", "Google")]
    [InlineData("short", "Google")]
    [InlineData("chirp", "Google")]
    [InlineData("chirp-v2", "Google")]
    [InlineData("medical_dictation", "Google")]
    [InlineData("gpt-4o-mini-transcribe", "OpenAI")]
    [InlineData("gpt-4o-transcribe", "OpenAI")]
    [InlineData("gpt-4o-transcribe-diarize", "OpenAI")]
    [InlineData("whisper", "Ollama")]
    [InlineData("whisper-small", "Ollama")]
    [InlineData("whisper-medium", "Ollama")]
    [InlineData("whisper-large-v3", "Ollama")]
    [InlineData("sense-voice-mini", "Ollama")]
    public void GetProviderForModel_TranscriberModels_ReturnCorrectProvider(string modelId, string expectedProvider)
    {
        // Arrange
        IOptionsMonitor<AiOptions> aiOptions = AiOptionsFactory.CreateWithAllProviders().ToMonitor();
        var registry = new ModelRegistry(aiOptions);

        // Act
        var provider = registry.GetProviderForModel(modelId);

        // Assert
        provider.Should().Be(expectedProvider);
    }

    [Fact]
    public void GetAvailableModels_ResponderModels_ContainExpectedOpenAiModels()
    {
        // Arrange
        IOptionsMonitor<AiOptions> aiOptions = AiOptionsFactory.CreateWithOpenAi().ToMonitor();
        var registry = new ModelRegistry(aiOptions);

        // Act
        AvailableModels availableModels = registry.GetAvailableModels();

        // Assert
        availableModels.ResponderModels.Should().Contain(m => m.Id == "gpt-5-2025-08-07");
        availableModels.ResponderModels.Should().Contain(m => m.Id == "gpt-5-mini-2025-08-07");
        availableModels.ResponderModels.Should().Contain(m => m.Id == "gpt-5-nano-2025-08-07");
    }

    [Fact]
    public void GetAvailableModels_TranscriberModels_ContainExpectedOpenAiModels()
    {
        // Arrange
        IOptionsMonitor<AiOptions> aiOptions = AiOptionsFactory.CreateWithOpenAi().ToMonitor();
        var registry = new ModelRegistry(aiOptions);

        // Act
        AvailableModels availableModels = registry.GetAvailableModels();

        // Assert
        availableModels.TranscriberModels.Should().Contain(m => m.Id == "gpt-4o-mini-transcribe");
        availableModels.TranscriberModels.Should().Contain(m => m.Id == "gpt-4o-transcribe");
        availableModels.TranscriberModels.Should().Contain(m => m.Id == "gpt-4o-transcribe-diarize");
    }
}
