namespace Intelligence.Tests;

/// <summary>
/// Unit tests for the <see cref="AvailableModels"/> record.
/// </summary>
public sealed class AvailableModelsTests
{
    [Fact]
    public void AvailableModels_DefaultValues_AreEmptyLists()
    {
        // Arrange & Act
        AvailableModels availableModels = new();

        // Assert
        _ = availableModels.TranscriberModels.Should().BeEmpty();
        _ = availableModels.ResponderModels.Should().BeEmpty();
    }

    [Fact]
    public void AvailableModels_WithTranscriberModels_SetsCorrectly()
    {
        // Arrange
        List<ModelInfo> transcriberModels =
        [
            new() { Id = "whisper", DisplayName = "Whisper", Provider = "Ollama" },
            new() { Id = "gpt-4o-mini-transcribe", DisplayName = "GPT-4o Mini Transcribe", Provider = "OpenAI" }
        ];

        // Act
        AvailableModels availableModels = new()
        {
            TranscriberModels = transcriberModels
        };

        // Assert
        _ = availableModels.TranscriberModels.Should().HaveCount(2);
        _ = availableModels.TranscriberModels[0].Id.Should().Be("whisper");
        _ = availableModels.TranscriberModels[1].Id.Should().Be("gpt-4o-mini-transcribe");
    }

    [Fact]
    public void AvailableModels_WithResponderModels_SetsCorrectly()
    {
        // Arrange
        List<ModelInfo> responderModels =
        [
            new() { Id = "gpt-5-nano-2025-08-07", DisplayName = "GPT-5 Nano", Provider = "OpenAI" },
            new() { Id = "gemini-2.5-pro", DisplayName = "Gemini 2.5 Pro", Provider = "Google" }
        ];

        // Act
        AvailableModels availableModels = new()
        {
            ResponderModels = responderModels
        };

        // Assert
        _ = availableModels.ResponderModels.Should().HaveCount(2);
        _ = availableModels.ResponderModels[0].Id.Should().Be("gpt-5-nano-2025-08-07");
        _ = availableModels.ResponderModels[1].Id.Should().Be("gemini-2.5-pro");
    }

    [Fact]
    public void AvailableModels_WithBothModelTypes_SetsCorrectly()
    {
        // Arrange
        List<ModelInfo> transcriberModels =
        [
            new() { Id = "whisper", DisplayName = "Whisper", Provider = "Ollama" }
        ];
        List<ModelInfo> responderModels =
        [
            new() { Id = "gpt-5-nano-2025-08-07", DisplayName = "GPT-5 Nano", Provider = "OpenAI" }
        ];

        // Act
        AvailableModels availableModels = new()
        {
            TranscriberModels = transcriberModels,
            ResponderModels = responderModels
        };

        // Assert
        _ = availableModels.TranscriberModels.Should().HaveCount(1);
        _ = availableModels.ResponderModels.Should().HaveCount(1);
    }

    [Fact]
    public void AvailableModels_Equality_WorksCorrectly()
    {
        // Arrange
        List<ModelInfo> models1 =
        [
            new() { Id = "model-1", DisplayName = "Model 1", Provider = "Provider" }
        ];
        AvailableModels availableModels1 = new()
        {
            TranscriberModels = models1,
            ResponderModels = models1
        };
        AvailableModels availableModels2 = new()
        {
            TranscriberModels = models1,
            ResponderModels = models1
        };

        // Assert
        _ = availableModels1.Should().Be(availableModels2);
    }

    [Fact]
    public void AvailableModels_Inequality_WhenTranscriberModelsDiffer()
    {
        // Arrange
        AvailableModels availableModels1 = new()
        {
            TranscriberModels = [new() { Id = "model-1", DisplayName = "Model 1", Provider = "Provider" }]
        };
        AvailableModels availableModels2 = new()
        {
            TranscriberModels = [new() { Id = "model-2", DisplayName = "Model 2", Provider = "Provider" }]
        };

        // Assert
        _ = availableModels1.Should().NotBe(availableModels2);
    }

    [Fact]
    public void AvailableModels_Inequality_WhenResponderModelsDiffer()
    {
        // Arrange
        AvailableModels availableModels1 = new()
        {
            ResponderModels = [new() { Id = "model-1", DisplayName = "Model 1", Provider = "Provider" }]
        };
        AvailableModels availableModels2 = new()
        {
            ResponderModels = [new() { Id = "model-2", DisplayName = "Model 2", Provider = "Provider" }]
        };

        // Assert
        _ = availableModels1.Should().NotBe(availableModels2);
    }

    [Fact]
    public void AvailableModels_WithCopy_CreatesNewInstance()
    {
        // Arrange
        AvailableModels original = new()
        {
            TranscriberModels = [new() { Id = "whisper", DisplayName = "Whisper", Provider = "Ollama" }],
            ResponderModels = []
        };

        // Act
        AvailableModels copy = original with
        {
            ResponderModels = [new() { Id = "gpt-5", DisplayName = "GPT-5", Provider = "OpenAI" }]
        };

        // Assert
        _ = copy.TranscriberModels.Should().HaveCount(1);
        _ = copy.ResponderModels.Should().HaveCount(1);
        _ = original.ResponderModels.Should().BeEmpty();
    }
}
