using Vitarish.Intelligence.Application.Abstractions;
using Vitarish.Intelligence.Infrastructure.Services.Dispatching;

namespace Intelligence.Tests;

/// <summary>
/// Unit tests for the <see cref="ProviderDispatchingResponder"/> class.
/// </summary>
public sealed class ProviderDispatchingResponderTests
{
    private const string DefaultResponseModel = "gpt-5-nano-2025-08-07";

    [Fact]
    public void Constructor_WithValidOptions_InitializesCorrectly()
    {
        // Arrange
        IOptionsMonitor<AiOptions> aiOptions = AiOptionsFactory.CreateWithOpenAi().ToMonitor();
        ModelRegistry modelRegistry = new(aiOptions);

        // Act
        ProviderDispatchingResponder responder = new(aiOptions, modelRegistry);

        // Assert
        _ = responder.Should().NotBeNull();
        _ = responder.Should().BeAssignableTo<IResponder>();
    }

    [Fact]
    public void StreamAsync_ReturnsAsyncEnumerable()
    {
        // Arrange
        IOptionsMonitor<AiOptions> aiOptions = AiOptionsFactory.CreateWithOpenAi().ToMonitor();
        ModelRegistry modelRegistry = new(aiOptions);
        ProviderDispatchingResponder responder = new(aiOptions, modelRegistry);
        List<ResponderInput> input = [ResponderInput.TextInput("Hello")];
        ResponderOptions options = new()
        { Model = DefaultResponseModel };

        // Act
        IAsyncEnumerable<ResponderOutput> stream = responder.StreamAsync(input, options);

        // Assert
        _ = stream.Should().NotBeNull();
    }

    [Theory]
    [InlineData("gpt-5-nano-2025-08-07")]
    [InlineData("gpt-5-2025-08-07")]
    [InlineData("gpt-5-mini-2025-08-07")]
    public void Constructor_WithOpenAiModels_InitializesCorrectly(string model)
    {
        // Arrange
        IOptionsMonitor<AiOptions> aiOptions = Options.Create(new AiOptions
        {
            DefaultResponseModel = model,
            OpenAi = new OpenAiOptions { ApiKey = "test-api-key" }
        }).ToMonitor();
        ModelRegistry modelRegistry = new(aiOptions);

        // Act
        ProviderDispatchingResponder responder = new(aiOptions, modelRegistry);

        // Assert
        _ = responder.Should().NotBeNull();
    }

    [Theory]
    [InlineData("gemini-2.5-pro")]
    [InlineData("gemini-2.5-flash")]
    [InlineData("gemini-2.5-flash-lite")]
    public void Constructor_WithGoogleModels_InitializesCorrectly(string model)
    {
        // Arrange
        IOptionsMonitor<AiOptions> aiOptions = Options.Create(new AiOptions
        {
            DefaultResponseModel = model,
            Google = new GoogleOptions
            {
                Gemini = new GoogleGeminiOptions { ApiKey = "gemini-api-key" }
            }
        }).ToMonitor();
        ModelRegistry modelRegistry = new(aiOptions);

        // Act
        ProviderDispatchingResponder responder = new(aiOptions, modelRegistry);

        // Assert
        _ = responder.Should().NotBeNull();
    }

    [Theory]
    [InlineData("qwen3:0.6b")]
    [InlineData("qwen3:1.7b")]
    [InlineData("qwen3:4b")]
    public void Constructor_WithOllamaModels_InitializesCorrectly(string model)
    {
        // Arrange
        IOptionsMonitor<AiOptions> aiOptions = Options.Create(new AiOptions
        {
            DefaultResponseModel = model,
            Ollama = new OllamaOptions { BaseUrl = "http://localhost:11434" }
        }).ToMonitor();
        ModelRegistry modelRegistry = new(aiOptions);

        // Act
        ProviderDispatchingResponder responder = new(aiOptions, modelRegistry);

        // Assert
        _ = responder.Should().NotBeNull();
    }
}
