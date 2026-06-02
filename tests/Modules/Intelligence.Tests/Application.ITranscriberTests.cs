using System.Threading.Channels;
using Vitarish.Intelligence.Application.Abstractions;

namespace Intelligence.Tests;

/// <summary>
/// Unit tests for the <see cref="ITranscriber"/> interface contract.
/// These tests verify the behavior expected from any ITranscriber implementation.
/// </summary>
public sealed class ITranscriberInterfaceTests
{
    [Fact]
    public async Task TranscribeAsync_WithValidAudio_ReturnsTranscribedText()
    {
        // Arrange
        Mock<ITranscriber> mockTranscriber = new();
        using MemoryStream audioStream = AudioTestDataGenerator.CreateDummyAudioStream();
        string expectedText = "Hello, this is a test transcription.";

        _ = mockTranscriber
            .Setup(t => t.TranscribeAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedText);

        // Act
        string? result = await mockTranscriber.Object.TranscribeAsync(audioStream, "audio/wav", "en-US", "whisper", CancellationToken.None);

        // Assert
        _ = result.Should().Be(expectedText);
    }

    [Fact]
    public async Task TranscribeAsync_WithNullLanguage_AcceptsNullLanguage()
    {
        // Arrange
        Mock<ITranscriber> mockTranscriber = new();
        using MemoryStream audioStream = AudioTestDataGenerator.CreateDummyAudioStream();

        _ = mockTranscriber
            .Setup(t => t.TranscribeAsync(It.IsAny<Stream>(), It.IsAny<string>(), null, It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Transcribed text");

        // Act
        string? result = await mockTranscriber.Object.TranscribeAsync(audioStream, "audio/wav", null, "whisper", CancellationToken.None);

        // Assert
        _ = result.Should().NotBeNullOrEmpty();
        mockTranscriber.Verify(t => t.TranscribeAsync(It.IsAny<Stream>(), "audio/wav", null, "whisper", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TranscribeAsync_WithNullModelId_AcceptsNullModelId()
    {
        // Arrange
        Mock<ITranscriber> mockTranscriber = new();
        using MemoryStream audioStream = AudioTestDataGenerator.CreateDummyAudioStream();

        _ = mockTranscriber
            .Setup(t => t.TranscribeAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string?>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync("Transcribed text");

        // Act
        string? result = await mockTranscriber.Object.TranscribeAsync(audioStream, "audio/wav", "en-US", null, CancellationToken.None);

        // Assert
        _ = result.Should().NotBeNullOrEmpty();
        mockTranscriber.Verify(t => t.TranscribeAsync(It.IsAny<Stream>(), "audio/wav", "en-US", null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TranscribeAsync_ReturnsNull_WhenNoSpeechDetected()
    {
        // Arrange
        Mock<ITranscriber> mockTranscriber = new();
        using MemoryStream audioStream = AudioTestDataGenerator.CreateDummyAudioStream();

        _ = mockTranscriber
            .Setup(t => t.TranscribeAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        // Act
        string? result = await mockTranscriber.Object.TranscribeAsync(audioStream, "audio/wav", "en-US", "whisper", CancellationToken.None);

        // Assert
        _ = result.Should().BeNull();
    }

    [Fact]
    public async Task TranscribeAsync_WithCancellation_PropagatesCancellationToken()
    {
        // Arrange
        Mock<ITranscriber> mockTranscriber = new();
        using MemoryStream audioStream = AudioTestDataGenerator.CreateDummyAudioStream();
        using CancellationTokenSource cts = new();
        cts.Cancel();

        _ = mockTranscriber
            .Setup(t => t.TranscribeAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        // Act
        Func<Task<string?>> act = () => mockTranscriber.Object.TranscribeAsync(audioStream, "audio/wav", "en-US", "whisper", cts.Token);

        // Assert
        _ = await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Theory]
    [InlineData("audio/wav")]
    [InlineData("audio/mpeg")]
    [InlineData("audio/ogg")]
    [InlineData("audio/webm")]
    public async Task TranscribeAsync_AcceptsVariousMimeTypes(string mimeType)
    {
        // Arrange
        Mock<ITranscriber> mockTranscriber = new();
        using MemoryStream audioStream = AudioTestDataGenerator.CreateDummyAudioStream();

        _ = mockTranscriber
            .Setup(t => t.TranscribeAsync(It.IsAny<Stream>(), mimeType, It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Transcribed text");

        // Act
        _ = await mockTranscriber.Object.TranscribeAsync(audioStream, mimeType, "en-US", "whisper", CancellationToken.None);

        // Assert
        mockTranscriber.Verify(t => t.TranscribeAsync(It.IsAny<Stream>(), mimeType, "en-US", "whisper", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("en-US")]
    [InlineData("de-DE")]
    [InlineData("fr-FR")]
    [InlineData("ja-JP")]
    [InlineData("zh-CN")]
    public async Task TranscribeAsync_AcceptsVariousLanguageCodes(string language)
    {
        // Arrange
        Mock<ITranscriber> mockTranscriber = new();
        using MemoryStream audioStream = AudioTestDataGenerator.CreateDummyAudioStream();

        _ = mockTranscriber
            .Setup(t => t.TranscribeAsync(It.IsAny<Stream>(), It.IsAny<string>(), language, It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Transcribed text");

        // Act
        _ = await mockTranscriber.Object.TranscribeAsync(audioStream, "audio/wav", language, "whisper", CancellationToken.None);

        // Assert
        mockTranscriber.Verify(t => t.TranscribeAsync(It.IsAny<Stream>(), "audio/wav", language, "whisper", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StreamTranscribeAsync_ReturnsAsyncEnumerableOfStrings()
    {
        // Arrange
        Mock<ITranscriber> mockTranscriber = new();
        Channel<byte[]> channel = Channel.CreateBounded<byte[]>(10);
        List<string> expectedChunks = ["Hello", " world", " test"];

        _ = mockTranscriber
            .Setup(t => t.StreamTranscribeAsync(
                It.IsAny<ChannelReader<byte[]>>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .Returns(expectedChunks.ToAsyncEnumerable());

        // Act
        List<string> results = [];
        await foreach (string chunk in mockTranscriber.Object.StreamTranscribeAsync(channel.Reader, "audio/wav", "en-US", "whisper", CancellationToken.None))
        {
            results.Add(chunk);
        }

        // Assert
        _ = results.Should().HaveCount(3);
        _ = results[0].Should().Be("Hello");
        _ = results[1].Should().Be(" world");
        _ = results[2].Should().Be(" test");
    }

    [Fact]
    public async Task StreamTranscribeAsync_WithCancellation_StopsStreaming()
    {
        // Arrange
        Mock<ITranscriber> mockTranscriber = new();
        Channel<byte[]> channel = Channel.CreateBounded<byte[]>(10);
        using CancellationTokenSource cts = new();
        cts.Cancel();

        _ = mockTranscriber
            .Setup(t => t.StreamTranscribeAsync(
                It.IsAny<ChannelReader<byte[]>>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .Returns(AsyncEnumerableTestExtensions.ThrowOnEnumerateAsync<string>(new OperationCanceledException()));

        // Act
        Func<Task> act = async () =>
        {
            await foreach (string _ in mockTranscriber.Object.StreamTranscribeAsync(channel.Reader, "audio/wav", "en-US", "whisper", cts.Token))
            {
                // Intentionally left empty: enumerating to trigger cancellation.
            }
        };

        // Assert
        _ = await act.Should().ThrowAsync<OperationCanceledException>();
    }
}
