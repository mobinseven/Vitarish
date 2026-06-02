namespace Intelligence.Tests;

/// <summary>
/// Unit tests for the <see cref="ModelInfo"/> record.
/// </summary>
public sealed class ModelInfoTests
{
    [Fact]
    public void ModelInfo_WithRequiredProperties_CreatesValidInstance()
    {
        // Arrange & Act
        ModelInfo modelInfo = new()
        {
            Id = "gpt-4o",
            DisplayName = "GPT-4o",
            Provider = "OpenAI"
        };

        // Assert
        _ = modelInfo.Id.Should().Be("gpt-4o");
        _ = modelInfo.DisplayName.Should().Be("GPT-4o");
        _ = modelInfo.Provider.Should().Be("OpenAI");
    }

    [Fact]
    public void ModelInfo_Equality_WorksCorrectly()
    {
        // Arrange
        ModelInfo modelInfo1 = new()
        {
            Id = "gpt-4o",
            DisplayName = "GPT-4o",
            Provider = "OpenAI"
        };
        ModelInfo modelInfo2 = new()
        {
            Id = "gpt-4o",
            DisplayName = "GPT-4o",
            Provider = "OpenAI"
        };

        // Assert
        _ = modelInfo1.Should().Be(modelInfo2);
    }

    [Fact]
    public void ModelInfo_Inequality_WhenIdDiffers()
    {
        // Arrange
        ModelInfo modelInfo1 = new()
        { Id = "gpt-4o", DisplayName = "GPT-4o", Provider = "OpenAI" };
        ModelInfo modelInfo2 = new()
        { Id = "gpt-5", DisplayName = "GPT-4o", Provider = "OpenAI" };

        // Assert
        _ = modelInfo1.Should().NotBe(modelInfo2);
    }

    [Fact]
    public void ModelInfo_Inequality_WhenDisplayNameDiffers()
    {
        // Arrange
        ModelInfo modelInfo1 = new()
        { Id = "gpt-4o", DisplayName = "GPT-4o", Provider = "OpenAI" };
        ModelInfo modelInfo2 = new()
        { Id = "gpt-4o", DisplayName = "GPT-4o Updated", Provider = "OpenAI" };

        // Assert
        _ = modelInfo1.Should().NotBe(modelInfo2);
    }

    [Fact]
    public void ModelInfo_Inequality_WhenProviderDiffers()
    {
        // Arrange
        ModelInfo modelInfo1 = new()
        { Id = "model-1", DisplayName = "Model 1", Provider = "OpenAI" };
        ModelInfo modelInfo2 = new()
        { Id = "model-1", DisplayName = "Model 1", Provider = "Google" };

        // Assert
        _ = modelInfo1.Should().NotBe(modelInfo2);
    }

    [Fact]
    public void ModelInfo_WithCopy_CreatesNewInstance()
    {
        // Arrange
        ModelInfo original = new()
        {
            Id = "gpt-4o",
            DisplayName = "GPT-4o",
            Provider = "OpenAI"
        };

        // Act
        ModelInfo copy = original with { DisplayName = "GPT-4o Updated" };

        // Assert
        _ = copy.Id.Should().Be("gpt-4o");
        _ = copy.DisplayName.Should().Be("GPT-4o Updated");
        _ = copy.Provider.Should().Be("OpenAI");
        _ = original.DisplayName.Should().Be("GPT-4o");
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
        ModelInfo modelInfo = new()
        {
            Id = id,
            DisplayName = "Test Model",
            Provider = "Test"
        };

        // Assert
        _ = modelInfo.Id.Should().Be(id);
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
        ModelInfo modelInfo = new()
        {
            Id = "test-model",
            DisplayName = "Test Model",
            Provider = provider
        };

        // Assert
        _ = modelInfo.Provider.Should().Be(provider);
    }
}
