using System.Threading.Channels;
using Vitarish.Intelligence.Application.Abstractions;

namespace Vitarish.Intelligence.Tests;

/// <summary>
/// Unit tests for the <see cref="ITranscriptionProvider"/> interface contract.
/// These tests verify the behavior expected from any ITranscriptionProvider implementation.
/// </summary>
public sealed class ITranscriptionProviderInterfaceTests
{
    [Fact]
    public void SupportsModel_WithSupportedModel_ReturnsTrue()
    {
        // Arrange
        var mockProvider = new Mock<ITranscriptionProvider>();
        mockProvider.Setup(p => p.SupportsModel("whisper")).Returns(true);

        // Act
        var result = mockProvider.Object.SupportsModel("whisper");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void SupportsModel_WithUnsupportedModel_ReturnsFalse()
    {
        // Arrange
        var mockProvider = new Mock<ITranscriptionProvider>();
        mockProvider.Setup(p => p.SupportsModel("unknown-model")).Returns(false);

        // Act
        var result = mockProvider.Object.SupportsModel("unknown-model");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void SupportsModel_WithNullModelId_ReturnsFalse()
    {
        // Arrange
        var mockProvider = new Mock<ITranscriptionProvider>();
        mockProvider.Setup(p => p.SupportsModel(null)).Returns(false);

        // Act
        var result = mockProvider.Object.SupportsModel(null);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task TranscribeAsync_WithValidInput_ReturnsTranscribedText()
    {
        // Arrange
        var mockProvider = new Mock<ITranscriptionProvider>();
        using var audioStream = AudioTestDataGenerator.CreateDummyAudioStream();
        var expectedText = "Test transcription result";

        mockProvider
            .Setup(p => p.TranscribeAsync(
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedText);

        // Act
        var result = await mockProvider.Object.TranscribeAsync(
            audioStream, "audio/wav", "en-US", "whisper", CancellationToken.None);

        // Assert
        result.Should().Be(expectedText);
    }

    [Fact]
    public async Task TranscribeAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        // Arrange
        var mockProvider = new Mock<ITranscriptionProvider>();
        using var audioStream = AudioTestDataGenerator.CreateDummyAudioStream();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        mockProvider
            .Setup(p => p.TranscribeAsync(
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        // Act
        var act = () => mockProvider.Object.TranscribeAsync(
            audioStream, "audio/wav", "en-US", "whisper", cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task StreamTranscribeAsync_ReturnsAsyncEnumerableOfStrings()
    {
        // Arrange
        var mockProvider = new Mock<ITranscriptionProvider>();
        var channel = Channel.CreateBounded<byte[]>(10);
        var expectedChunks = new List<string> { "Real", "-time", " transcription" };

        mockProvider
            .Setup(p => p.StreamTranscribeAsync(
                It.IsAny<ChannelReader<byte[]>>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .Returns(expectedChunks.ToAsyncEnumerable());

        // Act
        var results = new List<string>();
        await foreach (var chunk in mockProvider.Object.StreamTranscribeAsync(
            channel.Reader, "audio/wav", "en-US", "whisper", CancellationToken.None))
        {
            results.Add(chunk);
        }

        // Assert
        results.Should().HaveCount(3);
        results.Should().BeEquivalentTo(expectedChunks);
    }

    [Fact]
    public async Task StreamTranscribeAsync_WithCancellation_StopsStreaming()
    {
        // Arrange
        var mockProvider = new Mock<ITranscriptionProvider>();
        var channel = Channel.CreateBounded<byte[]>(10);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        mockProvider
            .Setup(p => p.StreamTranscribeAsync(
                It.IsAny<ChannelReader<byte[]>>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .Returns(AsyncEnumerableTestExtensions.ThrowOnEnumerateAsync<string>(new OperationCanceledException()));

        // Act
        var act = async () =>
        {
            await foreach (var _ in mockProvider.Object.StreamTranscribeAsync(
                channel.Reader, "audio/wav", "en-US", "whisper", cts.Token))
            {
                // Intentionally left empty: enumerating to trigger OperationCanceledException
            }
        };

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Theory]
    [InlineData("whisper")]
    [InlineData("gpt-4o-mini-transcribe")]
    [InlineData("chirp")]
    public void SupportsModel_WithVariousModels_VerifiesModelSupport(string modelId)
    {
        // Arrange
        var mockProvider = new Mock<ITranscriptionProvider>();
        mockProvider.Setup(p => p.SupportsModel(modelId)).Returns(true);

        // Act
        mockProvider.Object.SupportsModel(modelId);

        // Assert
        mockProvider.Verify(p => p.SupportsModel(modelId), Times.Once);
    }
}
