using System.Threading.Channels;
using Vitarish.Intelligence.Application.Abstractions;

namespace Intelligence.Tests;

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
        Mock<ITranscriptionProvider> mockProvider = new();
        _ = mockProvider.Setup(p => p.SupportsModel("whisper")).Returns(true);

        // Act
        bool result = mockProvider.Object.SupportsModel("whisper");

        // Assert
        _ = result.Should().BeTrue();
    }

    [Fact]
    public void SupportsModel_WithUnsupportedModel_ReturnsFalse()
    {
        // Arrange
        Mock<ITranscriptionProvider> mockProvider = new();
        _ = mockProvider.Setup(p => p.SupportsModel("unknown-model")).Returns(false);

        // Act
        bool result = mockProvider.Object.SupportsModel("unknown-model");

        // Assert
        _ = result.Should().BeFalse();
    }

    [Fact]
    public void SupportsModel_WithNullModelId_ReturnsFalse()
    {
        // Arrange
        Mock<ITranscriptionProvider> mockProvider = new();
        _ = mockProvider.Setup(p => p.SupportsModel(null)).Returns(false);

        // Act
        bool result = mockProvider.Object.SupportsModel(null);

        // Assert
        _ = result.Should().BeFalse();
    }

    [Fact]
    public async Task TranscribeAsync_WithValidInput_ReturnsTranscribedText()
    {
        // Arrange
        Mock<ITranscriptionProvider> mockProvider = new();
        using MemoryStream audioStream = AudioTestDataGenerator.CreateDummyAudioStream();
        string expectedText = "Test transcription result";

        _ = mockProvider
            .Setup(p => p.TranscribeAsync(
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedText);

        // Act
        string? result = await mockProvider.Object.TranscribeAsync(
            audioStream, "audio/wav", "en-US", "whisper", CancellationToken.None);

        // Assert
        _ = result.Should().Be(expectedText);
    }

    [Fact]
    public async Task TranscribeAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        // Arrange
        Mock<ITranscriptionProvider> mockProvider = new();
        using MemoryStream audioStream = AudioTestDataGenerator.CreateDummyAudioStream();
        using CancellationTokenSource cts = new();
        cts.Cancel();

        _ = mockProvider
            .Setup(p => p.TranscribeAsync(
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        // Act
        Func<Task<string?>> act = () => mockProvider.Object.TranscribeAsync(
            audioStream, "audio/wav", "en-US", "whisper", cts.Token);

        // Assert
        _ = await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task StreamTranscribeAsync_ReturnsAsyncEnumerableOfStrings()
    {
        // Arrange
        Mock<ITranscriptionProvider> mockProvider = new();
        Channel<byte[]> channel = Channel.CreateBounded<byte[]>(10);
        List<string> expectedChunks = ["Real", "-time", " transcription"];

        _ = mockProvider
            .Setup(p => p.StreamTranscribeAsync(
                It.IsAny<ChannelReader<byte[]>>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .Returns(expectedChunks.ToAsyncEnumerable());

        // Act
        List<string> results = [];
        await foreach (string chunk in mockProvider.Object.StreamTranscribeAsync(
            channel.Reader, "audio/wav", "en-US", "whisper", CancellationToken.None))
        {
            results.Add(chunk);
        }

        // Assert
        _ = results.Should().HaveCount(3);
        _ = results.Should().BeEquivalentTo(expectedChunks);
    }

    [Fact]
    public async Task StreamTranscribeAsync_WithCancellation_StopsStreaming()
    {
        // Arrange
        Mock<ITranscriptionProvider> mockProvider = new();
        Channel<byte[]> channel = Channel.CreateBounded<byte[]>(10);
        using CancellationTokenSource cts = new();
        cts.Cancel();

        _ = mockProvider
            .Setup(p => p.StreamTranscribeAsync(
                It.IsAny<ChannelReader<byte[]>>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .Returns(AsyncEnumerableTestExtensions.ThrowOnEnumerateAsync<string>(new OperationCanceledException()));

        // Act
        Func<Task> act = async () =>
        {
            await foreach (string _ in mockProvider.Object.StreamTranscribeAsync(
                channel.Reader, "audio/wav", "en-US", "whisper", cts.Token))
            {
                // Intentionally left empty: enumerating to trigger OperationCanceledException
            }
        };

        // Assert
        _ = await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Theory]
    [InlineData("whisper")]
    [InlineData("gpt-4o-mini-transcribe")]
    [InlineData("chirp")]
    public void SupportsModel_WithVariousModels_VerifiesModelSupport(string modelId)
    {
        // Arrange
        Mock<ITranscriptionProvider> mockProvider = new();
        _ = mockProvider.Setup(p => p.SupportsModel(modelId)).Returns(true);

        // Act
        _ = mockProvider.Object.SupportsModel(modelId);

        // Assert
        mockProvider.Verify(p => p.SupportsModel(modelId), Times.Once);
    }
}
