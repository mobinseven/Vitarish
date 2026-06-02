namespace Intelligence.Tests;

/// <summary>
/// Unit tests for the <see cref="ResponderOptions"/> record.
/// </summary>
public sealed class ResponderOptionsTests
{
    [Fact]
    public void ResponderOptions_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        ResponderOptions options = new();

        // Assert
        _ = options.Instructions.Should().BeNull();
        _ = options.TextFormat.Should().Be(ResponseTextFormat.Text);
        _ = options.PreviousResponseId.Should().BeNull();
        _ = options.Model.Should().BeNull();
        _ = options.Tools.Should().BeEmpty();
        _ = options.EnableWebSearchTool.Should().BeFalse();
        _ = options.EnableComputerTool.Should().BeFalse();
    }

    [Fact]
    public void ResponderOptions_WithInstructions_SetsCorrectly()
    {
        // Arrange & Act
        ResponderOptions options = new()
        {
            Instructions = "You are a helpful assistant."
        };

        // Assert
        _ = options.Instructions.Should().Be("You are a helpful assistant.");
    }

    [Fact]
    public void ResponderOptions_WithTextFormat_SetsCorrectly()
    {
        // Arrange & Act
        ResponderOptions options = new()
        {
            TextFormat = ResponseTextFormat.JsonObject
        };

        // Assert
        _ = options.TextFormat.Should().Be(ResponseTextFormat.JsonObject);
    }

    [Fact]
    public void ResponderOptions_WithPreviousResponseId_SetsCorrectly()
    {
        // Arrange & Act
        ResponderOptions options = new()
        {
            PreviousResponseId = "resp-12345"
        };

        // Assert
        _ = options.PreviousResponseId.Should().Be("resp-12345");
    }

    [Fact]
    public void ResponderOptions_WithModel_SetsCorrectly()
    {
        // Arrange & Act
        ResponderOptions options = new()
        {
            Model = "gpt-5-nano-2025-08-07"
        };

        // Assert
        _ = options.Model.Should().Be("gpt-5-nano-2025-08-07");
    }

    [Fact]
    public void ResponderOptions_WithTools_SetsCorrectly()
    {
        // Arrange
        List<ResponderToolDefinition> tools =
        [
            new()
            {
                Type = "mcp",
                ServerLabel = "web-search",
                ServerUrl = "https://mcp.example.com",
                AllowedTools = ["search", "browse"]
            }
        ];

        // Act
        ResponderOptions options = new()
        {
            Tools = tools
        };

        // Assert
        _ = options.Tools.Should().HaveCount(1);
        _ = options.Tools[0].Type.Should().Be("mcp");
        _ = options.Tools[0].ServerLabel.Should().Be("web-search");
    }

    [Fact]
    public void ResponderOptions_WithWebSearchEnabled_SetsCorrectly()
    {
        // Arrange & Act
        ResponderOptions options = new()
        {
            EnableWebSearchTool = true
        };

        // Assert
        _ = options.EnableWebSearchTool.Should().BeTrue();
    }

    [Fact]
    public void ResponderOptions_WithComputerToolEnabled_SetsCorrectly()
    {
        // Arrange & Act
        ResponderOptions options = new()
        {
            EnableComputerTool = true
        };

        // Assert
        _ = options.EnableComputerTool.Should().BeTrue();
    }

    [Fact]
    public void ResponderOptions_WithAllProperties_SetsCorrectly()
    {
        // Arrange
        List<ResponderToolDefinition> tools =
        [
            new() { Type = "mcp", ServerLabel = "server-1", ServerUrl = "https://mcp1.example.com" }
        ];

        // Act
        ResponderOptions options = new()
        {
            Instructions = "Be helpful",
            TextFormat = ResponseTextFormat.JsonObject,
            PreviousResponseId = "prev-123",
            Model = "gpt-5",
            Tools = tools,
            EnableWebSearchTool = true,
            EnableComputerTool = true
        };

        // Assert
        _ = options.Instructions.Should().Be("Be helpful");
        _ = options.TextFormat.Should().Be(ResponseTextFormat.JsonObject);
        _ = options.PreviousResponseId.Should().Be("prev-123");
        _ = options.Model.Should().Be("gpt-5");
        _ = options.Tools.Should().HaveCount(1);
        _ = options.EnableWebSearchTool.Should().BeTrue();
        _ = options.EnableComputerTool.Should().BeTrue();
    }

    [Fact]
    public void ResponderOptions_Equality_WorksCorrectly()
    {
        // Arrange
        ResponderOptions options1 = new()
        { Model = "gpt-5", Instructions = "Test" };
        ResponderOptions options2 = new()
        { Model = "gpt-5", Instructions = "Test" };

        // Assert
        _ = options1.Should().Be(options2);
    }

    [Fact]
    public void ResponderOptions_WithCopy_CreatesNewInstance()
    {
        // Arrange
        ResponderOptions original = new()
        { Model = "gpt-4", Instructions = "Original" };

        // Act
        ResponderOptions copy = original with { Model = "gpt-5" };

        // Assert
        _ = copy.Model.Should().Be("gpt-5");
        _ = copy.Instructions.Should().Be("Original");
        _ = original.Model.Should().Be("gpt-4");
    }

    [Theory]
    [InlineData("gpt-4o")]
    [InlineData("gpt-5-nano-2025-08-07")]
    [InlineData("gemini-2.5-pro")]
    [InlineData("llama3.2")]
    public void ResponderOptions_AcceptsVariousModels(string model)
    {
        // Arrange & Act
        ResponderOptions options = new()
        { Model = model };

        // Assert
        _ = options.Model.Should().Be(model);
    }
}

/// <summary>
/// Unit tests for the <see cref="ResponderToolDefinition"/> record.
/// </summary>
public sealed class ResponderToolDefinitionTests
{
    [Fact]
    public void ResponderToolDefinition_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        ResponderToolDefinition tool = new()
        { Type = "mcp" };

        // Assert
        _ = tool.Type.Should().Be("mcp");
        _ = tool.ServerLabel.Should().BeNull();
        _ = tool.ServerUrl.Should().BeNull();
        _ = tool.ServerToken.Should().BeNull();
        _ = tool.AllowedTools.Should().BeEmpty();
        _ = tool.RequireApproval.Should().Be(ResponderToolApprovalRequirement.Never);
    }

    [Fact]
    public void ResponderToolDefinition_WithAllProperties_SetsCorrectly()
    {
        // Arrange & Act
        ResponderToolDefinition tool = new()
        {
            Type = "mcp",
            ServerLabel = "web-server",
            ServerUrl = "https://mcp.example.com",
            ServerToken = "secret-token",
            AllowedTools = ["search", "browse", "read"],
            RequireApproval = ResponderToolApprovalRequirement.Always
        };

        // Assert
        _ = tool.Type.Should().Be("mcp");
        _ = tool.ServerLabel.Should().Be("web-server");
        _ = tool.ServerUrl.Should().Be("https://mcp.example.com");
        _ = tool.ServerToken.Should().Be("secret-token");
        _ = tool.AllowedTools.Should().HaveCount(3);
        _ = tool.RequireApproval.Should().Be(ResponderToolApprovalRequirement.Always);
    }

    [Fact]
    public void ResponderToolDefinition_Equality_WorksCorrectly()
    {
        // Arrange
        ResponderToolDefinition tool1 = new()
        { Type = "mcp", ServerLabel = "server-1" };
        ResponderToolDefinition tool2 = new()
        { Type = "mcp", ServerLabel = "server-1" };

        // Assert
        _ = tool1.Should().Be(tool2);
    }

    [Theory]
    [InlineData(ResponderToolApprovalRequirement.Never)]
    [InlineData(ResponderToolApprovalRequirement.Always)]
    public void ResponderToolDefinition_AcceptsAllApprovalRequirements(ResponderToolApprovalRequirement approval)
    {
        // Arrange & Act
        ResponderToolDefinition tool = new()
        {
            Type = "mcp",
            RequireApproval = approval
        };

        // Assert
        _ = tool.RequireApproval.Should().Be(approval);
    }
}

/// <summary>
/// Unit tests for the <see cref="ResponseTextFormat"/> enum.
/// </summary>
public sealed class ResponseTextFormatTests
{
    [Fact]
    public void ResponseTextFormat_HasExpectedValues()
    {
        // Assert
        _ = Enum.GetValues<ResponseTextFormat>().Should().Contain(ResponseTextFormat.Text);
        _ = Enum.GetValues<ResponseTextFormat>().Should().Contain(ResponseTextFormat.JsonObject);
    }

    [Theory]
    [InlineData(ResponseTextFormat.Text, 0)]
    [InlineData(ResponseTextFormat.JsonObject, 1)]
    public void ResponseTextFormat_HasCorrectIntValues(ResponseTextFormat format, int expectedValue) =>
        // Assert
        ((int)format).Should().Be(expectedValue);
}

/// <summary>
/// Unit tests for the <see cref="ResponderToolApprovalRequirement"/> enum.
/// </summary>
public sealed class ResponderToolApprovalRequirementTests
{
    [Fact]
    public void ResponderToolApprovalRequirement_HasExpectedValues()
    {
        // Assert
        _ = Enum.GetValues<ResponderToolApprovalRequirement>().Should().Contain(ResponderToolApprovalRequirement.Never);
        _ = Enum.GetValues<ResponderToolApprovalRequirement>().Should().Contain(ResponderToolApprovalRequirement.Always);
    }

    [Theory]
    [InlineData(ResponderToolApprovalRequirement.Never, 0)]
    [InlineData(ResponderToolApprovalRequirement.Always, 1)]
    public void ResponderToolApprovalRequirement_HasCorrectIntValues(ResponderToolApprovalRequirement requirement, int expectedValue) =>
        // Assert
        ((int)requirement).Should().Be(expectedValue);
}
