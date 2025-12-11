using Microsoft.Extensions.Options;
using Vitarish.Intelligence.Application.Abstractions;
using Vitarish.Intelligence.Infrastructure.Services.Dispatching;

namespace Vitarish.Intelligence.Tests;

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
        var aiOptions = AiOptionsFactory.CreateWithOpenAi().ToMonitor();
        var modelRegistry = new ModelRegistry(aiOptions);

        // Act
        var responder = new ProviderDispatchingResponder(aiOptions, modelRegistry);

        // Assert
        responder.Should().NotBeNull();
        responder.Should().BeAssignableTo<IResponder>();
    }

    [Fact]
    public void StreamAsync_ReturnsAsyncEnumerable()
    {
        // Arrange
        var aiOptions = AiOptionsFactory.CreateWithOpenAi().ToMonitor();
        var modelRegistry = new ModelRegistry(aiOptions);
        var responder = new ProviderDispatchingResponder(aiOptions, modelRegistry);
        var input = new List<ResponderInput> { ResponderInput.TextInput("Hello") };
        var options = new ResponderOptions { Model = DefaultResponseModel };

        // Act
        var stream = responder.StreamAsync(input, options);

        // Assert
        stream.Should().NotBeNull();
    }

    [Theory]
    [InlineData("gpt-5-nano-2025-08-07")]
    [InlineData("gpt-5-2025-08-07")]
    [InlineData("gpt-5-mini-2025-08-07")]
    public void Constructor_WithOpenAiModels_InitializesCorrectly(string model)
    {
        // Arrange
        var aiOptions = Options.Create(new AiOptions
        {
            DefaultResponseModel = model,
            OpenAi = new OpenAiOptions { ApiKey = "test-api-key" }
        }).ToMonitor();
        var modelRegistry = new ModelRegistry(aiOptions);

        // Act
        var responder = new ProviderDispatchingResponder(aiOptions, modelRegistry);

        // Assert
        responder.Should().NotBeNull();
    }

    [Theory]
    [InlineData("gemini-2.5-pro")]
    [InlineData("gemini-2.5-flash")]
    [InlineData("gemini-2.5-flash-lite")]
    public void Constructor_WithGoogleModels_InitializesCorrectly(string model)
    {
        // Arrange
        var aiOptions = Options.Create(new AiOptions
        {
            DefaultResponseModel = model,
            Google = new GoogleOptions
            {
                Gemini = new GoogleGeminiOptions { ApiKey = "gemini-api-key" }
            }
        }).ToMonitor();
        var modelRegistry = new ModelRegistry(aiOptions);

        // Act
        var responder = new ProviderDispatchingResponder(aiOptions, modelRegistry);

        // Assert
        responder.Should().NotBeNull();
    }

    [Theory]
    [InlineData("qwen3:0.6b")]
    [InlineData("qwen3:1.7b")]
    [InlineData("qwen3:4b")]
    public void Constructor_WithOllamaModels_InitializesCorrectly(string model)
    {
        // Arrange
        var aiOptions = Options.Create(new AiOptions
        {
            DefaultResponseModel = model,
            Ollama = new OllamaOptions { BaseUrl = "http://localhost:11434" }
        }).ToMonitor();
        var modelRegistry = new ModelRegistry(aiOptions);

        // Act
        var responder = new ProviderDispatchingResponder(aiOptions, modelRegistry);

        // Assert
        responder.Should().NotBeNull();
    }
}
