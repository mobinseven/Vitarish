namespace Vitarish.Intelligence.Tests;

/// <summary>
/// Unit tests for the <see cref="AvailableModels"/> record.
/// </summary>
public sealed class AvailableModelsTests
{
    [Fact]
    public void AvailableModels_DefaultValues_AreEmptyLists()
    {
        // Arrange & Act
        var availableModels = new AvailableModels();

        // Assert
        availableModels.TranscriberModels.Should().BeEmpty();
        availableModels.ResponderModels.Should().BeEmpty();
    }

    [Fact]
    public void AvailableModels_WithTranscriberModels_SetsCorrectly()
    {
        // Arrange
        var transcriberModels = new List<ModelInfo>
        {
            new() { Id = "whisper", DisplayName = "Whisper", Provider = "Ollama" },
            new() { Id = "gpt-4o-mini-transcribe", DisplayName = "GPT-4o Mini Transcribe", Provider = "OpenAI" }
        };

        // Act
        var availableModels = new AvailableModels
        {
            TranscriberModels = transcriberModels
        };

        // Assert
        availableModels.TranscriberModels.Should().HaveCount(2);
        availableModels.TranscriberModels[0].Id.Should().Be("whisper");
        availableModels.TranscriberModels[1].Id.Should().Be("gpt-4o-mini-transcribe");
    }

    [Fact]
    public void AvailableModels_WithResponderModels_SetsCorrectly()
    {
        // Arrange
        var responderModels = new List<ModelInfo>
        {
            new() { Id = "gpt-5-nano-2025-08-07", DisplayName = "GPT-5 Nano", Provider = "OpenAI" },
            new() { Id = "gemini-2.5-pro", DisplayName = "Gemini 2.5 Pro", Provider = "Google" }
        };

        // Act
        var availableModels = new AvailableModels
        {
            ResponderModels = responderModels
        };

        // Assert
        availableModels.ResponderModels.Should().HaveCount(2);
        availableModels.ResponderModels[0].Id.Should().Be("gpt-5-nano-2025-08-07");
        availableModels.ResponderModels[1].Id.Should().Be("gemini-2.5-pro");
    }

    [Fact]
    public void AvailableModels_WithBothModelTypes_SetsCorrectly()
    {
        // Arrange
        var transcriberModels = new List<ModelInfo>
        {
            new() { Id = "whisper", DisplayName = "Whisper", Provider = "Ollama" }
        };
        var responderModels = new List<ModelInfo>
        {
            new() { Id = "gpt-5-nano-2025-08-07", DisplayName = "GPT-5 Nano", Provider = "OpenAI" }
        };

        // Act
        var availableModels = new AvailableModels
        {
            TranscriberModels = transcriberModels,
            ResponderModels = responderModels
        };

        // Assert
        availableModels.TranscriberModels.Should().HaveCount(1);
        availableModels.ResponderModels.Should().HaveCount(1);
    }

    [Fact]
    public void AvailableModels_Equality_WorksCorrectly()
    {
        // Arrange
        var models1 = new List<ModelInfo>
        {
            new() { Id = "model-1", DisplayName = "Model 1", Provider = "Provider" }
        };
        var availableModels1 = new AvailableModels
        {
            TranscriberModels = models1,
            ResponderModels = models1
        };
        var availableModels2 = new AvailableModels
        {
            TranscriberModels = models1,
            ResponderModels = models1
        };

        // Assert
        availableModels1.Should().Be(availableModels2);
    }

    [Fact]
    public void AvailableModels_Inequality_WhenTranscriberModelsDiffer()
    {
        // Arrange
        var availableModels1 = new AvailableModels
        {
            TranscriberModels = [new() { Id = "model-1", DisplayName = "Model 1", Provider = "Provider" }]
        };
        var availableModels2 = new AvailableModels
        {
            TranscriberModels = [new() { Id = "model-2", DisplayName = "Model 2", Provider = "Provider" }]
        };

        // Assert
        availableModels1.Should().NotBe(availableModels2);
    }

    [Fact]
    public void AvailableModels_Inequality_WhenResponderModelsDiffer()
    {
        // Arrange
        var availableModels1 = new AvailableModels
        {
            ResponderModels = [new() { Id = "model-1", DisplayName = "Model 1", Provider = "Provider" }]
        };
        var availableModels2 = new AvailableModels
        {
            ResponderModels = [new() { Id = "model-2", DisplayName = "Model 2", Provider = "Provider" }]
        };

        // Assert
        availableModels1.Should().NotBe(availableModels2);
    }

    [Fact]
    public void AvailableModels_WithCopy_CreatesNewInstance()
    {
        // Arrange
        var original = new AvailableModels
        {
            TranscriberModels = [new() { Id = "whisper", DisplayName = "Whisper", Provider = "Ollama" }],
            ResponderModels = []
        };

        // Act
        var copy = original with
        {
            ResponderModels = [new() { Id = "gpt-5", DisplayName = "GPT-5", Provider = "OpenAI" }]
        };

        // Assert
        copy.TranscriberModels.Should().HaveCount(1);
        copy.ResponderModels.Should().HaveCount(1);
        original.ResponderModels.Should().BeEmpty();
    }
}
