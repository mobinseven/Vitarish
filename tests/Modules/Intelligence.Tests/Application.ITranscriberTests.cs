using System.Threading.Channels;
using Vitarish.Intelligence.Application.Abstractions;

namespace Vitarish.Intelligence.Tests;

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
        var mockTranscriber = new Mock<ITranscriber>();
        using var audioStream = AudioTestDataGenerator.CreateDummyAudioStream();
        var expectedText = "Hello, this is a test transcription.";

        mockTranscriber
            .Setup(t => t.TranscribeAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedText);

        // Act
        var result = await mockTranscriber.Object.TranscribeAsync(audioStream, "audio/wav", "en-US", "whisper", CancellationToken.None);

        // Assert
        result.Should().Be(expectedText);
    }

    [Fact]
    public async Task TranscribeAsync_WithNullLanguage_AcceptsNullLanguage()
    {
        // Arrange
        var mockTranscriber = new Mock<ITranscriber>();
        using var audioStream = AudioTestDataGenerator.CreateDummyAudioStream();

        mockTranscriber
            .Setup(t => t.TranscribeAsync(It.IsAny<Stream>(), It.IsAny<string>(), null, It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Transcribed text");

        // Act
        var result = await mockTranscriber.Object.TranscribeAsync(audioStream, "audio/wav", null, "whisper", CancellationToken.None);

        // Assert
        result.Should().NotBeNullOrEmpty();
        mockTranscriber.Verify(t => t.TranscribeAsync(It.IsAny<Stream>(), "audio/wav", null, "whisper", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TranscribeAsync_WithNullModelId_AcceptsNullModelId()
    {
        // Arrange
        var mockTranscriber = new Mock<ITranscriber>();
        using var audioStream = AudioTestDataGenerator.CreateDummyAudioStream();

        mockTranscriber
            .Setup(t => t.TranscribeAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string?>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync("Transcribed text");

        // Act
        var result = await mockTranscriber.Object.TranscribeAsync(audioStream, "audio/wav", "en-US", null, CancellationToken.None);

        // Assert
        result.Should().NotBeNullOrEmpty();
        mockTranscriber.Verify(t => t.TranscribeAsync(It.IsAny<Stream>(), "audio/wav", "en-US", null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TranscribeAsync_ReturnsNull_WhenNoSpeechDetected()
    {
        // Arrange
        var mockTranscriber = new Mock<ITranscriber>();
        using var audioStream = AudioTestDataGenerator.CreateDummyAudioStream();

        mockTranscriber
            .Setup(t => t.TranscribeAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        // Act
        var result = await mockTranscriber.Object.TranscribeAsync(audioStream, "audio/wav", "en-US", "whisper", CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task TranscribeAsync_WithCancellation_PropagatesCancellationToken()
    {
        // Arrange
        var mockTranscriber = new Mock<ITranscriber>();
        using var audioStream = AudioTestDataGenerator.CreateDummyAudioStream();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        mockTranscriber
            .Setup(t => t.TranscribeAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        // Act
        var act = () => mockTranscriber.Object.TranscribeAsync(audioStream, "audio/wav", "en-US", "whisper", cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Theory]
    [InlineData("audio/wav")]
    [InlineData("audio/mpeg")]
    [InlineData("audio/ogg")]
    [InlineData("audio/webm")]
    public async Task TranscribeAsync_AcceptsVariousMimeTypes(string mimeType)
    {
        // Arrange
        var mockTranscriber = new Mock<ITranscriber>();
        using var audioStream = AudioTestDataGenerator.CreateDummyAudioStream();

        mockTranscriber
            .Setup(t => t.TranscribeAsync(It.IsAny<Stream>(), mimeType, It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Transcribed text");

        // Act
        await mockTranscriber.Object.TranscribeAsync(audioStream, mimeType, "en-US", "whisper", CancellationToken.None);

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
        var mockTranscriber = new Mock<ITranscriber>();
        using var audioStream = AudioTestDataGenerator.CreateDummyAudioStream();

        mockTranscriber
            .Setup(t => t.TranscribeAsync(It.IsAny<Stream>(), It.IsAny<string>(), language, It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Transcribed text");

        // Act
        await mockTranscriber.Object.TranscribeAsync(audioStream, "audio/wav", language, "whisper", CancellationToken.None);

        // Assert
        mockTranscriber.Verify(t => t.TranscribeAsync(It.IsAny<Stream>(), "audio/wav", language, "whisper", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StreamTranscribeAsync_ReturnsAsyncEnumerableOfStrings()
    {
        // Arrange
        var mockTranscriber = new Mock<ITranscriber>();
        var channel = Channel.CreateBounded<byte[]>(10);
        var expectedChunks = new List<string> { "Hello", " world", " test" };

        mockTranscriber
            .Setup(t => t.StreamTranscribeAsync(
                It.IsAny<ChannelReader<byte[]>>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .Returns(expectedChunks.ToAsyncEnumerable());

        // Act
        var results = new List<string>();
        await foreach (var chunk in mockTranscriber.Object.StreamTranscribeAsync(channel.Reader, "audio/wav", "en-US", "whisper", CancellationToken.None))
        {
            results.Add(chunk);
        }

        // Assert
        results.Should().HaveCount(3);
        results[0].Should().Be("Hello");
        results[1].Should().Be(" world");
        results[2].Should().Be(" test");
    }

    [Fact]
    public async Task StreamTranscribeAsync_WithCancellation_StopsStreaming()
    {
        // Arrange
        var mockTranscriber = new Mock<ITranscriber>();
        var channel = Channel.CreateBounded<byte[]>(10);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        mockTranscriber
            .Setup(t => t.StreamTranscribeAsync(
                It.IsAny<ChannelReader<byte[]>>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .Returns(AsyncEnumerableTestExtensions.ThrowOnEnumerateAsync<string>(new OperationCanceledException()));

        // Act
        var act = async () =>
        {
            await foreach (var _ in mockTranscriber.Object.StreamTranscribeAsync(channel.Reader, "audio/wav", "en-US", "whisper", cts.Token))
            {
                // Intentionally left empty: enumerating to trigger cancellation.
            }
        };

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}
