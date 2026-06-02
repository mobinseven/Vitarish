namespace Intelligence.Tests;

/// <summary>
/// Unit tests for the <see cref="AiOptions"/> class.
/// </summary>
public sealed class AiOptionsTests
{
    [Fact]
    public void AiOptions_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        AiOptions options = new();

        // Assert
        _ = options.DefaultTranscriptionModel.Should().Be("gpt-4o-mini-transcribe");
        _ = options.DefaultResponseModel.Should().Be("gpt-5-nano-2025-08-07");
        _ = options.Ollama.Should().NotBeNull();
        _ = options.OpenAi.Should().NotBeNull();
        _ = options.Google.Should().NotBeNull();
        _ = options.QuickRewriteInstructions.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void AiOptions_DefaultTranscriptionModel_CanBeSet()
    {
        // Arrange
        AiOptions options = new()
        {
            // Act
            DefaultTranscriptionModel = "whisper-large-v3"
        };

        // Assert
        _ = options.DefaultTranscriptionModel.Should().Be("whisper-large-v3");
    }

    [Fact]
    public void AiOptions_DefaultResponseModel_CanBeSet()
    {
        // Arrange
        AiOptions options = new()
        {
            // Act
            DefaultResponseModel = "gpt-5-2025-08-07"
        };

        // Assert
        _ = options.DefaultResponseModel.Should().Be("gpt-5-2025-08-07");
    }

    [Fact]
    public void AiOptions_OllamaOptions_CanBeSet()
    {
        // Arrange
        AiOptions options = new();
        OllamaOptions ollamaOptions = new()
        {
            BaseUrl = "http://custom-ollama:11434",
            DefaultModel = "qwen3:4b"
        };

        // Act
        options.Ollama = ollamaOptions;

        // Assert
        _ = options.Ollama.Should().BeSameAs(ollamaOptions);
        _ = options.Ollama.BaseUrl.Should().Be("http://custom-ollama:11434");
    }

    [Fact]
    public void AiOptions_OpenAiOptions_CanBeSet()
    {
        // Arrange
        AiOptions options = new();
        OpenAiOptions openAiOptions = new()
        {
            ApiKey = "test-api-key"
        };

        // Act
        options.OpenAi = openAiOptions;

        // Assert
        _ = options.OpenAi.Should().BeSameAs(openAiOptions);
        _ = options.OpenAi.ApiKey.Should().Be("test-api-key");
    }

    [Fact]
    public void AiOptions_GoogleOptions_CanBeSet()
    {
        // Arrange
        AiOptions options = new();
        GoogleOptions googleOptions = new()
        {
            Gemini = new GoogleGeminiOptions { ApiKey = "gemini-key" },
            Speech = new GoogleSpeechOptions { ProjectId = "test-project" }
        };

        // Act
        options.Google = googleOptions;

        // Assert
        _ = options.Google.Should().BeSameAs(googleOptions);
        _ = options.Google.Gemini.ApiKey.Should().Be("gemini-key");
    }

    [Fact]
    public void AiOptions_QuickRewriteInstructions_CanBeSet()
    {
        // Arrange
        AiOptions options = new();
        string customInstructions = "Custom rewrite instructions";

        // Act
        options.QuickRewriteInstructions = customInstructions;

        // Assert
        _ = options.QuickRewriteInstructions.Should().Be(customInstructions);
    }

    [Theory]
    [InlineData("gpt-4o-mini-transcribe")]
    [InlineData("whisper")]
    [InlineData("whisper-large-v3")]
    [InlineData("gpt-4o-transcribe")]
    public void AiOptions_AcceptsVariousTranscriptionModels(string model)
    {
        // Arrange & Act
        AiOptions options = new()
        { DefaultTranscriptionModel = model };

        // Assert
        _ = options.DefaultTranscriptionModel.Should().Be(model);
    }

    [Theory]
    [InlineData("gpt-5-nano-2025-08-07")]
    [InlineData("gpt-5-2025-08-07")]
    [InlineData("gemini-2.5-pro")]
    [InlineData("llama3.2")]
    public void AiOptions_AcceptsVariousResponseModels(string model)
    {
        // Arrange & Act
        AiOptions options = new()
        { DefaultResponseModel = model };

        // Assert
        _ = options.DefaultResponseModel.Should().Be(model);
    }
}

/// <summary>
/// Unit tests for the <see cref="OpenAiOptions"/> class.
/// </summary>
public sealed class OpenAiOptionsTests
{
    [Fact]
    public void OpenAiOptions_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        OpenAiOptions options = new();

        // Assert
        _ = options.ApiKey.Should().BeEmpty();
    }

    [Fact]
    public void OpenAiOptions_ApiKey_CanBeSet()
    {
        // Arrange
        OpenAiOptions options = new()
        {
            // Act
            ApiKey = "sk-test-key-12345"
        };

        // Assert
        _ = options.ApiKey.Should().Be("sk-test-key-12345");
    }

    [Theory]
    [InlineData("")]
    [InlineData("sk-test-key")]
    [InlineData("sk-proj-very-long-api-key-12345")]
    public void OpenAiOptions_AcceptsVariousApiKeyFormats(string apiKey)
    {
        // Arrange & Act
        OpenAiOptions options = new()
        { ApiKey = apiKey };

        // Assert
        _ = options.ApiKey.Should().Be(apiKey);
    }
}

/// <summary>
/// Unit tests for the <see cref="OllamaOptions"/> class.
/// </summary>
public sealed class OllamaOptionsTests
{
    [Fact]
    public void OllamaOptions_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        OllamaOptions options = new();

        // Assert
        _ = options.BaseUrl.Should().Be("http://localhost:11434");
        _ = options.DefaultModel.Should().Be("llama3.2");
        _ = options.DefaultTranscriberModel.Should().Be("whisper");
    }

    [Fact]
    public void OllamaOptions_BaseUrl_CanBeSet()
    {
        // Arrange
        OllamaOptions options = new()
        {
            // Act
            BaseUrl = "http://custom-ollama:11434"
        };

        // Assert
        _ = options.BaseUrl.Should().Be("http://custom-ollama:11434");
    }

    [Fact]
    public void OllamaOptions_DefaultModel_CanBeSet()
    {
        // Arrange
        OllamaOptions options = new()
        {
            // Act
            DefaultModel = "qwen3:4b"
        };

        // Assert
        _ = options.DefaultModel.Should().Be("qwen3:4b");
    }

    [Fact]
    public void OllamaOptions_DefaultTranscriberModel_CanBeSet()
    {
        // Arrange
        OllamaOptions options = new()
        {
            // Act
            DefaultTranscriberModel = "whisper-large-v3"
        };

        // Assert
        _ = options.DefaultTranscriberModel.Should().Be("whisper-large-v3");
    }

    [Theory]
    [InlineData("http://localhost:11434")]
    [InlineData("http://ollama:11434")]
    [InlineData("http://192.168.1.100:11434")]
    [InlineData("https://ollama.example.com")]
    public void OllamaOptions_AcceptsVariousBaseUrls(string baseUrl)
    {
        // Arrange & Act
        OllamaOptions options = new()
        { BaseUrl = baseUrl };

        // Assert
        _ = options.BaseUrl.Should().Be(baseUrl);
    }

    [Theory]
    [InlineData("llama3.2")]
    [InlineData("qwen3:0.6b")]
    [InlineData("qwen3:4b")]
    [InlineData("mixtral")]
    public void OllamaOptions_AcceptsVariousModels(string model)
    {
        // Arrange & Act
        OllamaOptions options = new()
        { DefaultModel = model };

        // Assert
        _ = options.DefaultModel.Should().Be(model);
    }
}

/// <summary>
/// Unit tests for the <see cref="GoogleOptions"/> class.
/// </summary>
public sealed class GoogleOptionsTests
{
    [Fact]
    public void GoogleOptions_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        GoogleOptions options = new();

        // Assert
        _ = options.Speech.Should().NotBeNull();
        _ = options.Gemini.Should().NotBeNull();
    }

    [Fact]
    public void GoogleOptions_SpeechOptions_CanBeSet()
    {
        // Arrange
        GoogleOptions options = new();
        GoogleSpeechOptions speechOptions = new()
        {
            ProjectId = "test-project",
            Location = "europe-west1"
        };

        // Act
        options.Speech = speechOptions;

        // Assert
        _ = options.Speech.Should().BeSameAs(speechOptions);
        _ = options.Speech.ProjectId.Should().Be("test-project");
    }

    [Fact]
    public void GoogleOptions_GeminiOptions_CanBeSet()
    {
        // Arrange
        GoogleOptions options = new();
        GoogleGeminiOptions geminiOptions = new()
        {
            ApiKey = "gemini-api-key"
        };

        // Act
        options.Gemini = geminiOptions;

        // Assert
        _ = options.Gemini.Should().BeSameAs(geminiOptions);
        _ = options.Gemini.ApiKey.Should().Be("gemini-api-key");
    }
}

/// <summary>
/// Unit tests for the <see cref="GoogleGeminiOptions"/> class.
/// </summary>
public sealed class GoogleGeminiOptionsTests
{
    [Fact]
    public void GoogleGeminiOptions_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        GoogleGeminiOptions options = new();

        // Assert
        _ = options.ApiKey.Should().BeEmpty();
    }

    [Fact]
    public void GoogleGeminiOptions_ApiKey_CanBeSet()
    {
        // Arrange
        GoogleGeminiOptions options = new()
        {
            // Act
            ApiKey = "test-gemini-api-key"
        };

        // Assert
        _ = options.ApiKey.Should().Be("test-gemini-api-key");
    }

    [Theory]
    [InlineData("")]
    [InlineData("test-key")]
    [InlineData("very-long-gemini-api-key-12345")]
    public void GoogleGeminiOptions_AcceptsVariousApiKeyFormats(string apiKey)
    {
        // Arrange & Act
        GoogleGeminiOptions options = new()
        { ApiKey = apiKey };

        // Assert
        _ = options.ApiKey.Should().Be(apiKey);
    }
}

/// <summary>
/// Unit tests for the <see cref="GoogleSpeechOptions"/> class.
/// </summary>
public sealed class GoogleSpeechOptionsTests
{
    [Fact]
    public void GoogleSpeechOptions_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        GoogleSpeechOptions options = new();

        // Assert
        _ = options.ProjectId.Should().BeEmpty();
        _ = options.Location.Should().Be("us-central1");
        _ = options.RecognizerId.Should().Be("default");
        _ = options.DefaultLanguageCode.Should().Be("en-US");
        _ = options.DefaultModelId.Should().Be("long");
        _ = options.SupportedModels.Should().Contain("long");
        _ = options.SupportedModels.Should().Contain("short");
        _ = options.SupportedModels.Should().Contain("chirp");
        _ = options.SupportedModels.Should().Contain("chirp-v2");
        _ = options.SupportedModels.Should().Contain("medical_dictation");
    }

    [Fact]
    public void GoogleSpeechOptions_ProjectId_CanBeSet()
    {
        // Arrange
        GoogleSpeechOptions options = new()
        {
            // Act
            ProjectId = "my-project"
        };

        // Assert
        _ = options.ProjectId.Should().Be("my-project");
    }

    [Fact]
    public void GoogleSpeechOptions_Location_CanBeSet()
    {
        // Arrange
        GoogleSpeechOptions options = new()
        {
            // Act
            Location = "europe-west1"
        };

        // Assert
        _ = options.Location.Should().Be("europe-west1");
    }

    [Fact]
    public void GoogleSpeechOptions_RecognizerId_CanBeSet()
    {
        // Arrange
        GoogleSpeechOptions options = new()
        {
            // Act
            RecognizerId = "custom-recognizer"
        };

        // Assert
        _ = options.RecognizerId.Should().Be("custom-recognizer");
    }

    [Fact]
    public void GoogleSpeechOptions_CredentialsPath_CanBeSet()
    {
        // Arrange
        GoogleSpeechOptions options = new()
        {
            // Act
            CredentialsPath = "/path/to/credentials.json"
        };

        // Assert
        _ = options.CredentialsPath.Should().Be("/path/to/credentials.json");
    }

    [Fact]
    public void GoogleSpeechOptions_DefaultLanguageCode_CanBeSet()
    {
        // Arrange
        GoogleSpeechOptions options = new()
        {
            // Act
            DefaultLanguageCode = "de-DE"
        };

        // Assert
        _ = options.DefaultLanguageCode.Should().Be("de-DE");
    }

    [Fact]
    public void GoogleSpeechOptions_DefaultModelId_CanBeSet()
    {
        // Arrange
        GoogleSpeechOptions options = new()
        {
            // Act
            DefaultModelId = "chirp"
        };

        // Assert
        _ = options.DefaultModelId.Should().Be("chirp");
    }

    [Fact]
    public void GoogleSpeechOptions_SupportedModels_CanBeSet()
    {
        // Arrange
        GoogleSpeechOptions options = new();
        string[] customModels = new[] { "custom-model-1", "custom-model-2" };

        // Act
        options.SupportedModels = customModels;

        // Assert
        _ = options.SupportedModels.Should().BeEquivalentTo(customModels);
    }

    [Theory]
    [InlineData("us-central1")]
    [InlineData("europe-west1")]
    [InlineData("asia-east1")]
    [InlineData("australia-southeast1")]
    public void GoogleSpeechOptions_AcceptsVariousLocations(string location)
    {
        // Arrange & Act
        GoogleSpeechOptions options = new()
        { Location = location };

        // Assert
        _ = options.Location.Should().Be(location);
    }

    [Theory]
    [InlineData("en-US")]
    [InlineData("de-DE")]
    [InlineData("fr-FR")]
    [InlineData("ja-JP")]
    public void GoogleSpeechOptions_AcceptsVariousLanguageCodes(string languageCode)
    {
        // Arrange & Act
        GoogleSpeechOptions options = new()
        { DefaultLanguageCode = languageCode };

        // Assert
        _ = options.DefaultLanguageCode.Should().Be(languageCode);
    }
}

/// <summary>
/// Unit tests for the <see cref="TranscriberOptions"/> record.
/// </summary>
public sealed class TranscriberOptionsTests
{
    [Fact]
    public void TranscriberOptions_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        TranscriberOptions options = new();

        // Assert
        _ = options.ModelId.Should().BeNull();
    }

    [Fact]
    public void TranscriberOptions_WithModelId_SetsCorrectly()
    {
        // Arrange & Act
        TranscriberOptions options = new()
        {
            ModelId = "whisper-large-v3"
        };

        // Assert
        _ = options.ModelId.Should().Be("whisper-large-v3");
    }

    [Fact]
    public void TranscriberOptions_Equality_WorksCorrectly()
    {
        // Arrange
        TranscriberOptions options1 = new()
        { ModelId = "whisper" };
        TranscriberOptions options2 = new()
        { ModelId = "whisper" };

        // Assert
        _ = options1.Should().Be(options2);
    }

    [Fact]
    public void TranscriberOptions_Inequality_WhenModelIdDiffers()
    {
        // Arrange
        TranscriberOptions options1 = new()
        { ModelId = "whisper" };
        TranscriberOptions options2 = new()
        { ModelId = "gpt-4o-transcribe" };

        // Assert
        _ = options1.Should().NotBe(options2);
    }

    [Theory]
    [InlineData("whisper")]
    [InlineData("whisper-large-v3")]
    [InlineData("gpt-4o-mini-transcribe")]
    [InlineData("long")]
    [InlineData("chirp")]
    public void TranscriberOptions_AcceptsVariousModelIds(string modelId)
    {
        // Arrange & Act
        TranscriberOptions options = new()
        { ModelId = modelId };

        // Assert
        _ = options.ModelId.Should().Be(modelId);
    }
}
