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
        var options = new AiOptions();

        // Assert
        options.DefaultTranscriptionModel.Should().Be("gpt-4o-mini-transcribe");
        options.DefaultResponseModel.Should().Be("gpt-5-nano-2025-08-07");
        options.Ollama.Should().NotBeNull();
        options.OpenAi.Should().NotBeNull();
        options.Google.Should().NotBeNull();
        options.QuickRewriteInstructions.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void AiOptions_DefaultTranscriptionModel_CanBeSet()
    {
        // Arrange
        var options = new AiOptions();

        // Act
        options.DefaultTranscriptionModel = "whisper-large-v3";

        // Assert
        options.DefaultTranscriptionModel.Should().Be("whisper-large-v3");
    }

    [Fact]
    public void AiOptions_DefaultResponseModel_CanBeSet()
    {
        // Arrange
        var options = new AiOptions();

        // Act
        options.DefaultResponseModel = "gpt-5-2025-08-07";

        // Assert
        options.DefaultResponseModel.Should().Be("gpt-5-2025-08-07");
    }

    [Fact]
    public void AiOptions_OllamaOptions_CanBeSet()
    {
        // Arrange
        var options = new AiOptions();
        var ollamaOptions = new OllamaOptions
        {
            BaseUrl = "http://custom-ollama:11434",
            DefaultModel = "qwen3:4b"
        };

        // Act
        options.Ollama = ollamaOptions;

        // Assert
        options.Ollama.Should().BeSameAs(ollamaOptions);
        options.Ollama.BaseUrl.Should().Be("http://custom-ollama:11434");
    }

    [Fact]
    public void AiOptions_OpenAiOptions_CanBeSet()
    {
        // Arrange
        var options = new AiOptions();
        var openAiOptions = new OpenAiOptions
        {
            ApiKey = "test-api-key"
        };

        // Act
        options.OpenAi = openAiOptions;

        // Assert
        options.OpenAi.Should().BeSameAs(openAiOptions);
        options.OpenAi.ApiKey.Should().Be("test-api-key");
    }

    [Fact]
    public void AiOptions_GoogleOptions_CanBeSet()
    {
        // Arrange
        var options = new AiOptions();
        var googleOptions = new GoogleOptions
        {
            Gemini = new GoogleGeminiOptions { ApiKey = "gemini-key" },
            Speech = new GoogleSpeechOptions { ProjectId = "test-project" }
        };

        // Act
        options.Google = googleOptions;

        // Assert
        options.Google.Should().BeSameAs(googleOptions);
        options.Google.Gemini.ApiKey.Should().Be("gemini-key");
    }

    [Fact]
    public void AiOptions_QuickRewriteInstructions_CanBeSet()
    {
        // Arrange
        var options = new AiOptions();
        var customInstructions = "Custom rewrite instructions";

        // Act
        options.QuickRewriteInstructions = customInstructions;

        // Assert
        options.QuickRewriteInstructions.Should().Be(customInstructions);
    }

    [Theory]
    [InlineData("gpt-4o-mini-transcribe")]
    [InlineData("whisper")]
    [InlineData("whisper-large-v3")]
    [InlineData("gpt-4o-transcribe")]
    public void AiOptions_AcceptsVariousTranscriptionModels(string model)
    {
        // Arrange & Act
        var options = new AiOptions { DefaultTranscriptionModel = model };

        // Assert
        options.DefaultTranscriptionModel.Should().Be(model);
    }

    [Theory]
    [InlineData("gpt-5-nano-2025-08-07")]
    [InlineData("gpt-5-2025-08-07")]
    [InlineData("gemini-2.5-pro")]
    [InlineData("llama3.2")]
    public void AiOptions_AcceptsVariousResponseModels(string model)
    {
        // Arrange & Act
        var options = new AiOptions { DefaultResponseModel = model };

        // Assert
        options.DefaultResponseModel.Should().Be(model);
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
        var options = new OpenAiOptions();

        // Assert
        options.ApiKey.Should().BeEmpty();
    }

    [Fact]
    public void OpenAiOptions_ApiKey_CanBeSet()
    {
        // Arrange
        var options = new OpenAiOptions();

        // Act
        options.ApiKey = "sk-test-key-12345";

        // Assert
        options.ApiKey.Should().Be("sk-test-key-12345");
    }

    [Theory]
    [InlineData("")]
    [InlineData("sk-test-key")]
    [InlineData("sk-proj-very-long-api-key-12345")]
    public void OpenAiOptions_AcceptsVariousApiKeyFormats(string apiKey)
    {
        // Arrange & Act
        var options = new OpenAiOptions { ApiKey = apiKey };

        // Assert
        options.ApiKey.Should().Be(apiKey);
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
        var options = new OllamaOptions();

        // Assert
        options.BaseUrl.Should().Be("http://localhost:11434");
        options.DefaultModel.Should().Be("llama3.2");
        options.DefaultTranscriberModel.Should().Be("whisper");
    }

    [Fact]
    public void OllamaOptions_BaseUrl_CanBeSet()
    {
        // Arrange
        var options = new OllamaOptions();

        // Act
        options.BaseUrl = "http://custom-ollama:11434";

        // Assert
        options.BaseUrl.Should().Be("http://custom-ollama:11434");
    }

    [Fact]
    public void OllamaOptions_DefaultModel_CanBeSet()
    {
        // Arrange
        var options = new OllamaOptions();

        // Act
        options.DefaultModel = "qwen3:4b";

        // Assert
        options.DefaultModel.Should().Be("qwen3:4b");
    }

    [Fact]
    public void OllamaOptions_DefaultTranscriberModel_CanBeSet()
    {
        // Arrange
        var options = new OllamaOptions();

        // Act
        options.DefaultTranscriberModel = "whisper-large-v3";

        // Assert
        options.DefaultTranscriberModel.Should().Be("whisper-large-v3");
    }

    [Theory]
    [InlineData("http://localhost:11434")]
    [InlineData("http://ollama:11434")]
    [InlineData("http://192.168.1.100:11434")]
    [InlineData("https://ollama.example.com")]
    public void OllamaOptions_AcceptsVariousBaseUrls(string baseUrl)
    {
        // Arrange & Act
        var options = new OllamaOptions { BaseUrl = baseUrl };

        // Assert
        options.BaseUrl.Should().Be(baseUrl);
    }

    [Theory]
    [InlineData("llama3.2")]
    [InlineData("qwen3:0.6b")]
    [InlineData("qwen3:4b")]
    [InlineData("mixtral")]
    public void OllamaOptions_AcceptsVariousModels(string model)
    {
        // Arrange & Act
        var options = new OllamaOptions { DefaultModel = model };

        // Assert
        options.DefaultModel.Should().Be(model);
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
        var options = new GoogleOptions();

        // Assert
        options.Speech.Should().NotBeNull();
        options.Gemini.Should().NotBeNull();
    }

    [Fact]
    public void GoogleOptions_SpeechOptions_CanBeSet()
    {
        // Arrange
        var options = new GoogleOptions();
        var speechOptions = new GoogleSpeechOptions
        {
            ProjectId = "test-project",
            Location = "europe-west1"
        };

        // Act
        options.Speech = speechOptions;

        // Assert
        options.Speech.Should().BeSameAs(speechOptions);
        options.Speech.ProjectId.Should().Be("test-project");
    }

    [Fact]
    public void GoogleOptions_GeminiOptions_CanBeSet()
    {
        // Arrange
        var options = new GoogleOptions();
        var geminiOptions = new GoogleGeminiOptions
        {
            ApiKey = "gemini-api-key"
        };

        // Act
        options.Gemini = geminiOptions;

        // Assert
        options.Gemini.Should().BeSameAs(geminiOptions);
        options.Gemini.ApiKey.Should().Be("gemini-api-key");
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
        var options = new GoogleGeminiOptions();

        // Assert
        options.ApiKey.Should().BeEmpty();
    }

    [Fact]
    public void GoogleGeminiOptions_ApiKey_CanBeSet()
    {
        // Arrange
        var options = new GoogleGeminiOptions();

        // Act
        options.ApiKey = "test-gemini-api-key";

        // Assert
        options.ApiKey.Should().Be("test-gemini-api-key");
    }

    [Theory]
    [InlineData("")]
    [InlineData("test-key")]
    [InlineData("very-long-gemini-api-key-12345")]
    public void GoogleGeminiOptions_AcceptsVariousApiKeyFormats(string apiKey)
    {
        // Arrange & Act
        var options = new GoogleGeminiOptions { ApiKey = apiKey };

        // Assert
        options.ApiKey.Should().Be(apiKey);
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
        var options = new GoogleSpeechOptions();

        // Assert
        options.ProjectId.Should().BeEmpty();
        options.Location.Should().Be("us-central1");
        options.RecognizerId.Should().Be("default");
        options.DefaultLanguageCode.Should().Be("en-US");
        options.DefaultModelId.Should().Be("long");
        options.SupportedModels.Should().Contain("long");
        options.SupportedModels.Should().Contain("short");
        options.SupportedModels.Should().Contain("chirp");
        options.SupportedModels.Should().Contain("chirp-v2");
        options.SupportedModels.Should().Contain("medical_dictation");
    }

    [Fact]
    public void GoogleSpeechOptions_ProjectId_CanBeSet()
    {
        // Arrange
        var options = new GoogleSpeechOptions();

        // Act
        options.ProjectId = "my-project";

        // Assert
        options.ProjectId.Should().Be("my-project");
    }

    [Fact]
    public void GoogleSpeechOptions_Location_CanBeSet()
    {
        // Arrange
        var options = new GoogleSpeechOptions();

        // Act
        options.Location = "europe-west1";

        // Assert
        options.Location.Should().Be("europe-west1");
    }

    [Fact]
    public void GoogleSpeechOptions_RecognizerId_CanBeSet()
    {
        // Arrange
        var options = new GoogleSpeechOptions();

        // Act
        options.RecognizerId = "custom-recognizer";

        // Assert
        options.RecognizerId.Should().Be("custom-recognizer");
    }

    [Fact]
    public void GoogleSpeechOptions_CredentialsPath_CanBeSet()
    {
        // Arrange
        var options = new GoogleSpeechOptions();

        // Act
        options.CredentialsPath = "/path/to/credentials.json";

        // Assert
        options.CredentialsPath.Should().Be("/path/to/credentials.json");
    }

    [Fact]
    public void GoogleSpeechOptions_DefaultLanguageCode_CanBeSet()
    {
        // Arrange
        var options = new GoogleSpeechOptions();

        // Act
        options.DefaultLanguageCode = "de-DE";

        // Assert
        options.DefaultLanguageCode.Should().Be("de-DE");
    }

    [Fact]
    public void GoogleSpeechOptions_DefaultModelId_CanBeSet()
    {
        // Arrange
        var options = new GoogleSpeechOptions();

        // Act
        options.DefaultModelId = "chirp";

        // Assert
        options.DefaultModelId.Should().Be("chirp");
    }

    [Fact]
    public void GoogleSpeechOptions_SupportedModels_CanBeSet()
    {
        // Arrange
        var options = new GoogleSpeechOptions();
        var customModels = new[] { "custom-model-1", "custom-model-2" };

        // Act
        options.SupportedModels = customModels;

        // Assert
        options.SupportedModels.Should().BeEquivalentTo(customModels);
    }

    [Theory]
    [InlineData("us-central1")]
    [InlineData("europe-west1")]
    [InlineData("asia-east1")]
    [InlineData("australia-southeast1")]
    public void GoogleSpeechOptions_AcceptsVariousLocations(string location)
    {
        // Arrange & Act
        var options = new GoogleSpeechOptions { Location = location };

        // Assert
        options.Location.Should().Be(location);
    }

    [Theory]
    [InlineData("en-US")]
    [InlineData("de-DE")]
    [InlineData("fr-FR")]
    [InlineData("ja-JP")]
    public void GoogleSpeechOptions_AcceptsVariousLanguageCodes(string languageCode)
    {
        // Arrange & Act
        var options = new GoogleSpeechOptions { DefaultLanguageCode = languageCode };

        // Assert
        options.DefaultLanguageCode.Should().Be(languageCode);
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
        var options = new TranscriberOptions();

        // Assert
        options.ModelId.Should().BeNull();
    }

    [Fact]
    public void TranscriberOptions_WithModelId_SetsCorrectly()
    {
        // Arrange & Act
        var options = new TranscriberOptions
        {
            ModelId = "whisper-large-v3"
        };

        // Assert
        options.ModelId.Should().Be("whisper-large-v3");
    }

    [Fact]
    public void TranscriberOptions_Equality_WorksCorrectly()
    {
        // Arrange
        var options1 = new TranscriberOptions { ModelId = "whisper" };
        var options2 = new TranscriberOptions { ModelId = "whisper" };

        // Assert
        options1.Should().Be(options2);
    }

    [Fact]
    public void TranscriberOptions_Inequality_WhenModelIdDiffers()
    {
        // Arrange
        var options1 = new TranscriberOptions { ModelId = "whisper" };
        var options2 = new TranscriberOptions { ModelId = "gpt-4o-transcribe" };

        // Assert
        options1.Should().NotBe(options2);
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
        var options = new TranscriberOptions { ModelId = modelId };

        // Assert
        options.ModelId.Should().Be(modelId);
    }
}
