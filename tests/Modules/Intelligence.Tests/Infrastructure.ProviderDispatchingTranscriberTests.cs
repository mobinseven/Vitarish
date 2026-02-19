using Microsoft.Extensions.Options;
using System.Threading.Tasks;
using Vitarish.Intelligence.Application.Abstractions;
using Vitarish.Intelligence.Infrastructure.Services.Dispatching;

namespace Intelligence.Tests;

/// <summary>
/// Unit tests for the <see cref="ProviderDispatchingTranscriber"/> class.
/// </summary>
public sealed class ProviderDispatchingTranscriberTests
{
    private const string DefaultTranscriptionModel = "gpt-4o-mini-transcribe";

    [Fact]
    public void Constructor_InitializesCorrectly()
    {
        // Arrange
        IOptionsMonitor<AiOptions> aiOptions = AiOptionsFactory.CreateWithOpenAi().ToMonitor();
        var modelRegistry = new ModelRegistry(aiOptions);

        // Act
        var transcriber = new ProviderDispatchingTranscriber(aiOptions, modelRegistry);

        // Assert
        transcriber.Should().NotBeNull();
        transcriber.Should().BeAssignableTo<ITranscriber>();
    }

    [Fact]
    public async Task TranscribeAsync_WithInvalidModel_ThrowsInvalidOperationException()
    {
        // Arrange
        IOptionsMonitor<AiOptions> aiOptions = AiOptionsFactory.CreateWithOpenAi().ToMonitor();
        var modelRegistry = new ModelRegistry(aiOptions);
        var transcriber = new ProviderDispatchingTranscriber(aiOptions, modelRegistry);
        using MemoryStream audioStream = AudioTestDataGenerator.CreateDummyAudioStream();

        // Act
        Func<Task<string?>> act = () => transcriber.TranscribeAsync(audioStream, "audio/wav", null, "unknown-model", CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not supported*");
    }

    [Theory]
    [InlineData("gpt-4o-mini-transcribe")]
    [InlineData("gpt-4o-transcribe")]
    [InlineData("gpt-4o-transcribe-diarize")]
    public void Constructor_WithOpenAiTranscriptionModels_InitializesCorrectly(string model)
    {
        // Arrange
        IOptionsMonitor<AiOptions> aiOptions = Options.Create(new AiOptions
        {
            DefaultTranscriptionModel = model,
            OpenAi = new OpenAiOptions { ApiKey = "test-api-key" }
        }).ToMonitor();
        var modelRegistry = new ModelRegistry(aiOptions);

        // Act
        var transcriber = new ProviderDispatchingTranscriber(aiOptions, modelRegistry);

        // Assert
        transcriber.Should().NotBeNull();
    }

    [Theory]
    [InlineData("long")]
    [InlineData("short")]
    [InlineData("chirp")]
    [InlineData("chirp-v2")]
    public void Constructor_WithGoogleTranscriptionModels_InitializesCorrectly(string model)
    {
        // Arrange
        IOptionsMonitor<AiOptions> aiOptions = Options.Create(new AiOptions
        {
            DefaultTranscriptionModel = model,
            Google = new GoogleOptions
            {
                Speech = new GoogleSpeechOptions
                {
                    ProjectId = "test-project",
                    Location = "us-central1",
                    RecognizerId = "default"
                }
            }
        }).ToMonitor();
        var modelRegistry = new ModelRegistry(aiOptions);

        // Act
        var transcriber = new ProviderDispatchingTranscriber(aiOptions, modelRegistry);

        // Assert
        transcriber.Should().NotBeNull();
    }

    [Theory]
    [InlineData("whisper")]
    [InlineData("whisper-small")]
    [InlineData("whisper-medium")]
    [InlineData("whisper-large-v3")]
    public void Constructor_WithOllamaTranscriptionModels_InitializesCorrectly(string model)
    {
        // Arrange
        IOptionsMonitor<AiOptions> aiOptions = Options.Create(new AiOptions
        {
            DefaultTranscriptionModel = model,
            Ollama = new OllamaOptions { BaseUrl = "http://localhost:11434" }
        }).ToMonitor();
        var modelRegistry = new ModelRegistry(aiOptions);

        // Act
        var transcriber = new ProviderDispatchingTranscriber(aiOptions, modelRegistry);

        // Assert
        transcriber.Should().NotBeNull();
    }

    [Fact]
    public async Task StreamTranscribeAsync_ReturnsAsyncEnumerable()
    {
        // Arrange
        IOptionsMonitor<AiOptions> aiOptions = AiOptionsFactory.CreateWithOpenAi().ToMonitor();
        var modelRegistry = new ModelRegistry(aiOptions);
        var transcriber = new ProviderDispatchingTranscriber(aiOptions, modelRegistry);
        var channel = System.Threading.Channels.Channel.CreateBounded<byte[]>(10);

        // Act
        IAsyncEnumerable<string> stream = transcriber.StreamTranscribeAsync(
            channel.Reader,
            "audio/wav",
            "en-US",
            DefaultTranscriptionModel,
            CancellationToken.None);
        // Assert
        stream.Should().NotBeNull();
    }
}
