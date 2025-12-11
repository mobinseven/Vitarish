namespace Vitarish.Intelligence.Tests;

/// <summary>
/// Unit tests for the <see cref="ModelInfo"/> record.
/// </summary>
public sealed class ModelInfoTests
{
    [Fact]
    public void ModelInfo_WithRequiredProperties_CreatesValidInstance()
    {
        // Arrange & Act
        var modelInfo = new ModelInfo
        {
            Id = "gpt-4o",
            DisplayName = "GPT-4o",
            Provider = "OpenAI"
        };

        // Assert
        modelInfo.Id.Should().Be("gpt-4o");
        modelInfo.DisplayName.Should().Be("GPT-4o");
        modelInfo.Provider.Should().Be("OpenAI");
    }

    [Fact]
    public void ModelInfo_Equality_WorksCorrectly()
    {
        // Arrange
        var modelInfo1 = new ModelInfo
        {
            Id = "gpt-4o",
            DisplayName = "GPT-4o",
            Provider = "OpenAI"
        };
        var modelInfo2 = new ModelInfo
        {
            Id = "gpt-4o",
            DisplayName = "GPT-4o",
            Provider = "OpenAI"
        };

        // Assert
        modelInfo1.Should().Be(modelInfo2);
    }

    [Fact]
    public void ModelInfo_Inequality_WhenIdDiffers()
    {
        // Arrange
        var modelInfo1 = new ModelInfo { Id = "gpt-4o", DisplayName = "GPT-4o", Provider = "OpenAI" };
        var modelInfo2 = new ModelInfo { Id = "gpt-5", DisplayName = "GPT-4o", Provider = "OpenAI" };

        // Assert
        modelInfo1.Should().NotBe(modelInfo2);
    }

    [Fact]
    public void ModelInfo_Inequality_WhenDisplayNameDiffers()
    {
        // Arrange
        var modelInfo1 = new ModelInfo { Id = "gpt-4o", DisplayName = "GPT-4o", Provider = "OpenAI" };
        var modelInfo2 = new ModelInfo { Id = "gpt-4o", DisplayName = "GPT-4o Updated", Provider = "OpenAI" };

        // Assert
        modelInfo1.Should().NotBe(modelInfo2);
    }

    [Fact]
    public void ModelInfo_Inequality_WhenProviderDiffers()
    {
        // Arrange
        var modelInfo1 = new ModelInfo { Id = "model-1", DisplayName = "Model 1", Provider = "OpenAI" };
        var modelInfo2 = new ModelInfo { Id = "model-1", DisplayName = "Model 1", Provider = "Google" };

        // Assert
        modelInfo1.Should().NotBe(modelInfo2);
    }

    [Fact]
    public void ModelInfo_WithCopy_CreatesNewInstance()
    {
        // Arrange
        var original = new ModelInfo
        {
            Id = "gpt-4o",
            DisplayName = "GPT-4o",
            Provider = "OpenAI"
        };

        // Act
        var copy = original with { DisplayName = "GPT-4o Updated" };

        // Assert
        copy.Id.Should().Be("gpt-4o");
        copy.DisplayName.Should().Be("GPT-4o Updated");
        copy.Provider.Should().Be("OpenAI");
        original.DisplayName.Should().Be("GPT-4o");
    }

    [Theory]
    [InlineData("gpt-4o")]
    [InlineData("gpt-4o-mini-transcribe")]
    [InlineData("gemini-2.5-pro")]
    [InlineData("llama3.2")]
    [InlineData("whisper-large-v3")]
    public void ModelInfo_AcceptsVariousModelIds(string id)
    {
        // Arrange & Act
        var modelInfo = new ModelInfo
        {
            Id = id,
            DisplayName = "Test Model",
            Provider = "Test"
        };

        // Assert
        modelInfo.Id.Should().Be(id);
    }

    [Theory]
    [InlineData("OpenAI")]
    [InlineData("Google")]
    [InlineData("Ollama")]
    [InlineData("Azure")]
    [InlineData("Anthropic")]
    public void ModelInfo_AcceptsVariousProviders(string provider)
    {
        // Arrange & Act
        var modelInfo = new ModelInfo
        {
            Id = "test-model",
            DisplayName = "Test Model",
            Provider = provider
        };

        // Assert
        modelInfo.Provider.Should().Be(provider);
    }
}
