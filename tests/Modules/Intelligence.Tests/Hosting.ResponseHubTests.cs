using System.Threading.Channels;
using System.Threading.Tasks;
using Vitarish.Intelligence.Application.Abstractions;
using Vitarish.Intelligence.Hosting.Hubs;
using Vitarish.Intelligence.Infrastructure.Services.Dispatching;

namespace Vitarish.Intelligence.Tests;

/// <summary>
/// Unit tests for the <see cref="ResponseHub"/> class.
/// </summary>
public sealed class ResponseHubTests
{
    [Fact]
    public async Task GetAvailableModelsAsync_ReturnsAvailableModels()
    {
        // Arrange
        var aiOptions = AiOptionsFactory.CreateWithOpenAi().ToMonitor();
        var modelRegistry = new ModelRegistry(aiOptions);
        var mockResponder = new Mock<IResponder>();
        var hub = new ResponseHub(mockResponder.Object, aiOptions, modelRegistry);

        // Act
        var result = await hub.GetAvailableModelsAsync();

        // Assert
        result.Should().NotBeNull();
        result.ResponderModels.Should().NotBeEmpty();
        result.TranscriberModels.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetAvailableModelsAsync_WhenOpenAiConfigured_ReturnsOpenAiModels()
    {
        // Arrange
        var aiOptions = AiOptionsFactory.CreateWithOpenAi().ToMonitor();
        var modelRegistry = new ModelRegistry(aiOptions);
        var mockResponder = new Mock<IResponder>();
        var hub = new ResponseHub(mockResponder.Object, aiOptions, modelRegistry);

        // Act
        var result = await hub.GetAvailableModelsAsync();

        // Assert
        result.ResponderModels.Should().Contain(m => m.Provider == "OpenAI");
        result.TranscriberModels.Should().Contain(m => m.Provider == "OpenAI");
    }

    [Fact]
    public async Task GetAvailableModelsAsync_WhenNoProvidersConfigured_ReturnsEmptyModels()
    {
        // Arrange
        var aiOptions = Options.Create(new AiOptions
        {
            OpenAi = new OpenAiOptions { ApiKey = "" },
            Ollama = new OllamaOptions { BaseUrl = "" },
            Google = new GoogleOptions
            {
                Gemini = new GoogleGeminiOptions { ApiKey = "" },
                Speech = new GoogleSpeechOptions { ProjectId = "" }
            }
        }).ToMonitor();
        var modelRegistry = new ModelRegistry(aiOptions);
        var mockResponder = new Mock<IResponder>();
        var hub = new ResponseHub(mockResponder.Object, aiOptions, modelRegistry);

        // Act
        var result = await hub.GetAvailableModelsAsync();

        // Assert
        result.ResponderModels.Should().BeEmpty();
        result.TranscriberModels.Should().BeEmpty();
    }

    [Fact]
    public async Task StreamAsync_CallsResponderStreamAsync()
    {
        // Arrange
        var aiOptions = AiOptionsFactory.CreateWithOpenAi().ToMonitor();
        var modelRegistry = new ModelRegistry(aiOptions);
        var mockResponder = new Mock<IResponder>();
        var input = new List<ResponderInput> { ResponderInput.TextInput("Test") };
        var options = new ResponderOptions();
        var expectedOutputs = new List<ResponderOutput> { ResponderOutput.TextOutput("Response") };

        mockResponder
            .Setup(r => r.StreamAsync(It.IsAny<IReadOnlyList<ResponderInput>>(), It.IsAny<ResponderOptions>(), It.IsAny<CancellationToken>()))
            .Returns(expectedOutputs.ToAsyncEnumerable());

        var hub = new ResponseHub(mockResponder.Object, aiOptions, modelRegistry);

        // Act
        var resultS = hub.StreamAsync(input, options, CancellationToken.None);
        var result = await resultS.ToListAsync();
        // Assert
        result.Should().NotBeNull();
        mockResponder.Verify(r => r.StreamAsync(input, options, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StreamAsync_YieldsAllResponderOutputs()
    {
        // Arrange
        var aiOptions = AiOptionsFactory.CreateWithOpenAi().ToMonitor();
        var modelRegistry = new ModelRegistry(aiOptions);
        var mockResponder = new Mock<IResponder>();
        var input = new List<ResponderInput> { ResponderInput.TextInput("Test") };
        var options = new ResponderOptions();
        var expectedOutputs = new List<ResponderOutput>
        {
            ResponderOutput.TextOutput("Hello"),
            ResponderOutput.TextOutput(" "),
            ResponderOutput.TextOutput("World")
        };

        mockResponder
            .Setup(r => r.StreamAsync(It.IsAny<IReadOnlyList<ResponderInput>>(), It.IsAny<ResponderOptions>(), It.IsAny<CancellationToken>()))
            .Returns(expectedOutputs.ToAsyncEnumerable());

        var hub = new ResponseHub(mockResponder.Object, aiOptions, modelRegistry);

        // Act
        var results = new List<ResponderOutput>();
        await foreach (var output in hub.StreamAsync(input, options, CancellationToken.None))
        {
            results.Add(output);
        }

        // Assert
        results.Should().HaveCount(3);
        results[0].Text.Should().Be("Hello");
        results[2].Text.Should().Be("World");
    }

    [Fact]
    public async Task StreamRewriteAsync_UsesQuickRewriteInstructions()
    {
        // Arrange
        var customInstructions = "Custom rewrite instructions for testing";
        var aiOptions = Options.Create(new AiOptions
        {
            QuickRewriteInstructions = customInstructions,
            OpenAi = new OpenAiOptions { ApiKey = "test-key" }
        }).ToMonitor();
        var modelRegistry = new ModelRegistry(aiOptions);
        var mockResponder = new Mock<IResponder>();

        ResponderOptions? capturedOptions = null;
        mockResponder
            .Setup(r => r.StreamAsync(
                It.IsAny<IReadOnlyList<ResponderInput>>(),
                It.IsAny<ResponderOptions>(),
                It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyList<ResponderInput>, ResponderOptions, CancellationToken>((_, opts, _) => capturedOptions = opts)
            .Returns(new List<ResponderOutput>().ToAsyncEnumerable());

        var hub = new ResponseHub(mockResponder.Object, aiOptions, modelRegistry);

        // Act
        var resultStream = hub.StreamRewriteAsync("Test prompt", CancellationToken.None);

        var result = await resultStream.ToListAsync();  // <- consume the stream
        // Assert
        result.Should().NotBeNull();
        mockResponder.Verify(r => r.StreamAsync(
            It.Is<IReadOnlyList<ResponderInput>>(i => i.Count == 1 && i[0].Text == "Test prompt"),
            It.Is<ResponderOptions>(o => o.Instructions == customInstructions),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StreamRewriteAsync_FiltersNonTextOutputs()
    {
        // Arrange
        var aiOptions = AiOptionsFactory.CreateWithOpenAi().ToMonitor();
        var modelRegistry = new ModelRegistry(aiOptions);
        var mockResponder = new Mock<IResponder>();
        var expectedOutputs = new List<ResponderOutput>
        {
            ResponderOutput.TextOutput("Rewritten text"),
            ResponderOutput.ReasoningOutput("Internal reasoning"),
            ResponderOutput.TextOutput(" more text"),
            ResponderOutput.CompletionOutput("id", [])
        };

        mockResponder
            .Setup(r => r.StreamAsync(It.IsAny<IReadOnlyList<ResponderInput>>(), It.IsAny<ResponderOptions>(), It.IsAny<CancellationToken>()))
            .Returns(expectedOutputs.ToAsyncEnumerable());

        var hub = new ResponseHub(mockResponder.Object, aiOptions, modelRegistry);

        // Act
        var results = new List<ResponderOutput>();
        await foreach (var output in hub.StreamRewriteAsync("Original text", CancellationToken.None))
        {
            results.Add(output);
        }

        // Assert - Only Text outputs should be returned
        results.Should().HaveCount(2);
        results.Should().OnlyContain(o => o.Type == ResponderOutputType.Text);
    }

    [Fact]
    public void Constructor_InitializesCorrectly()
    {
        // Arrange
        var aiOptions = AiOptionsFactory.CreateWithOpenAi().ToMonitor();
        var modelRegistry = new ModelRegistry(aiOptions);
        var mockResponder = new Mock<IResponder>();

        // Act
        var hub = new ResponseHub(mockResponder.Object, aiOptions, modelRegistry);

        // Assert
        hub.Should().NotBeNull();
    }
}
