using System.Threading.Channels;
using System.Threading.Tasks;
using Vitarish.Intelligence.Application.Abstractions;
using Vitarish.Intelligence.Hosting.Hubs;
using Vitarish.Intelligence.Infrastructure.Services.Dispatching;

namespace Vitarish.Intelligence.Tests;

/// <summary>
/// Unit tests for the <see cref="SpeechHub"/> class.
/// </summary>
public sealed class SpeechHubTests
{
    [Fact]
    public async Task GetAvailableModelsAsync_ReturnsAvailableModels()
    {
        // Arrange
        var aiOptions = AiOptionsFactory.CreateWithOpenAi().ToMonitor();
        var modelRegistry = new ModelRegistry(aiOptions);
        var mockTranscriber = new Mock<ITranscriber>();
        var hub = new SpeechHub(mockTranscriber.Object, modelRegistry);

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
        var mockTranscriber = new Mock<ITranscriber>();
        var hub = new SpeechHub(mockTranscriber.Object, modelRegistry);

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
        var mockTranscriber = new Mock<ITranscriber>();
        var hub = new SpeechHub(mockTranscriber.Object, modelRegistry);

        // Act
        var result = await hub.GetAvailableModelsAsync();

        // Assert
        result.ResponderModels.Should().BeEmpty();
        result.TranscriberModels.Should().BeEmpty();
    }

    [Fact]
    public async Task StreamTranscribeAsync_CallsTranscriberStreamTranscribeAsync()
    {
        // Arrange
        var aiOptions = AiOptionsFactory.CreateWithOpenAi().ToMonitor();
        var modelRegistry = new ModelRegistry(aiOptions);
        var mockTranscriber = new Mock<ITranscriber>();
        var channel = Channel.CreateBounded<byte[]>(10);
        var options = new TranscriberOptions { ModelId = "whisper" };
        var expectedChunks = new List<string> { "Hello", " world" };

        mockTranscriber
            .Setup(t => t.StreamTranscribeAsync(
                It.IsAny<ChannelReader<byte[]>>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .Returns(expectedChunks.ToAsyncEnumerable());

        var hub = new SpeechHub(mockTranscriber.Object, modelRegistry);

        // Act
        var resultStream = hub.StreamTranscribeAsync(channel.Reader, "audio/wav", "en-US", options, CancellationToken.None, CancellationToken.None);
        var result = await resultStream.ToListAsync();  // <- consume the stream

        // Assert
        mockTranscriber.Verify(t => t.StreamTranscribeAsync(
            channel.Reader,
            "audio/wav",
            "en-US",
            options.ModelId,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StreamTranscribeAsync_YieldsAllTranscriptionChunks()
    {
        // Arrange
        var aiOptions = AiOptionsFactory.CreateWithOpenAi().ToMonitor();
        var modelRegistry = new ModelRegistry(aiOptions);
        var mockTranscriber = new Mock<ITranscriber>();
        var channel = Channel.CreateBounded<byte[]>(10);
        var options = new TranscriberOptions { ModelId = "whisper" };
        var expectedChunks = new List<string> { "First", " chunk", " third" };

        mockTranscriber
            .Setup(t => t.StreamTranscribeAsync(
                It.IsAny<ChannelReader<byte[]>>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .Returns(expectedChunks.ToAsyncEnumerable());

        var hub = new SpeechHub(mockTranscriber.Object, modelRegistry);

        // Act
        var results = new List<string>();
        await foreach (var chunk in hub.StreamTranscribeAsync(channel.Reader, "audio/wav", "en-US", options, CancellationToken.None, CancellationToken.None))
        {
            results.Add(chunk);
        }

        // Assert
        results.Should().HaveCount(3);
        results[0].Should().Be("First");
        results[1].Should().Be(" chunk");
        results[2].Should().Be(" third");
    }

    [Fact]
    public void Constructor_InitializesCorrectly()
    {
        // Arrange
        var aiOptions = AiOptionsFactory.CreateWithOpenAi().ToMonitor();
        var modelRegistry = new ModelRegistry(aiOptions);
        var mockTranscriber = new Mock<ITranscriber>();

        // Act
        var hub = new SpeechHub(mockTranscriber.Object, modelRegistry);

        // Assert
        hub.Should().NotBeNull();
    }
}
