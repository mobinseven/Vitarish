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
        var options = new ResponderOptions();

        // Assert
        options.Instructions.Should().BeNull();
        options.TextFormat.Should().Be(ResponseTextFormat.Text);
        options.PreviousResponseId.Should().BeNull();
        options.Model.Should().BeNull();
        options.Tools.Should().BeEmpty();
        options.EnableWebSearchTool.Should().BeFalse();
        options.EnableComputerTool.Should().BeFalse();
    }

    [Fact]
    public void ResponderOptions_WithInstructions_SetsCorrectly()
    {
        // Arrange & Act
        var options = new ResponderOptions
        {
            Instructions = "You are a helpful assistant."
        };

        // Assert
        options.Instructions.Should().Be("You are a helpful assistant.");
    }

    [Fact]
    public void ResponderOptions_WithTextFormat_SetsCorrectly()
    {
        // Arrange & Act
        var options = new ResponderOptions
        {
            TextFormat = ResponseTextFormat.JsonObject
        };

        // Assert
        options.TextFormat.Should().Be(ResponseTextFormat.JsonObject);
    }

    [Fact]
    public void ResponderOptions_WithPreviousResponseId_SetsCorrectly()
    {
        // Arrange & Act
        var options = new ResponderOptions
        {
            PreviousResponseId = "resp-12345"
        };

        // Assert
        options.PreviousResponseId.Should().Be("resp-12345");
    }

    [Fact]
    public void ResponderOptions_WithModel_SetsCorrectly()
    {
        // Arrange & Act
        var options = new ResponderOptions
        {
            Model = "gpt-5-nano-2025-08-07"
        };

        // Assert
        options.Model.Should().Be("gpt-5-nano-2025-08-07");
    }

    [Fact]
    public void ResponderOptions_WithTools_SetsCorrectly()
    {
        // Arrange
        var tools = new List<ResponderToolDefinition>
        {
            new()
            {
                Type = "mcp",
                ServerLabel = "web-search",
                ServerUrl = "https://mcp.example.com",
                AllowedTools = ["search", "browse"]
            }
        };

        // Act
        var options = new ResponderOptions
        {
            Tools = tools
        };

        // Assert
        options.Tools.Should().HaveCount(1);
        options.Tools[0].Type.Should().Be("mcp");
        options.Tools[0].ServerLabel.Should().Be("web-search");
    }

    [Fact]
    public void ResponderOptions_WithWebSearchEnabled_SetsCorrectly()
    {
        // Arrange & Act
        var options = new ResponderOptions
        {
            EnableWebSearchTool = true
        };

        // Assert
        options.EnableWebSearchTool.Should().BeTrue();
    }

    [Fact]
    public void ResponderOptions_WithComputerToolEnabled_SetsCorrectly()
    {
        // Arrange & Act
        var options = new ResponderOptions
        {
            EnableComputerTool = true
        };

        // Assert
        options.EnableComputerTool.Should().BeTrue();
    }

    [Fact]
    public void ResponderOptions_WithAllProperties_SetsCorrectly()
    {
        // Arrange
        var tools = new List<ResponderToolDefinition>
        {
            new() { Type = "mcp", ServerLabel = "server-1", ServerUrl = "https://mcp1.example.com" }
        };

        // Act
        var options = new ResponderOptions
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
        options.Instructions.Should().Be("Be helpful");
        options.TextFormat.Should().Be(ResponseTextFormat.JsonObject);
        options.PreviousResponseId.Should().Be("prev-123");
        options.Model.Should().Be("gpt-5");
        options.Tools.Should().HaveCount(1);
        options.EnableWebSearchTool.Should().BeTrue();
        options.EnableComputerTool.Should().BeTrue();
    }

    [Fact]
    public void ResponderOptions_Equality_WorksCorrectly()
    {
        // Arrange
        var options1 = new ResponderOptions { Model = "gpt-5", Instructions = "Test" };
        var options2 = new ResponderOptions { Model = "gpt-5", Instructions = "Test" };

        // Assert
        options1.Should().Be(options2);
    }

    [Fact]
    public void ResponderOptions_WithCopy_CreatesNewInstance()
    {
        // Arrange
        var original = new ResponderOptions { Model = "gpt-4", Instructions = "Original" };

        // Act
        ResponderOptions copy = original with { Model = "gpt-5" };

        // Assert
        copy.Model.Should().Be("gpt-5");
        copy.Instructions.Should().Be("Original");
        original.Model.Should().Be("gpt-4");
    }

    [Theory]
    [InlineData("gpt-4o")]
    [InlineData("gpt-5-nano-2025-08-07")]
    [InlineData("gemini-2.5-pro")]
    [InlineData("llama3.2")]
    public void ResponderOptions_AcceptsVariousModels(string model)
    {
        // Arrange & Act
        var options = new ResponderOptions { Model = model };

        // Assert
        options.Model.Should().Be(model);
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
        var tool = new ResponderToolDefinition { Type = "mcp" };

        // Assert
        tool.Type.Should().Be("mcp");
        tool.ServerLabel.Should().BeNull();
        tool.ServerUrl.Should().BeNull();
        tool.ServerToken.Should().BeNull();
        tool.AllowedTools.Should().BeEmpty();
        tool.RequireApproval.Should().Be(ResponderToolApprovalRequirement.Never);
    }

    [Fact]
    public void ResponderToolDefinition_WithAllProperties_SetsCorrectly()
    {
        // Arrange & Act
        var tool = new ResponderToolDefinition
        {
            Type = "mcp",
            ServerLabel = "web-server",
            ServerUrl = "https://mcp.example.com",
            ServerToken = "secret-token",
            AllowedTools = ["search", "browse", "read"],
            RequireApproval = ResponderToolApprovalRequirement.Always
        };

        // Assert
        tool.Type.Should().Be("mcp");
        tool.ServerLabel.Should().Be("web-server");
        tool.ServerUrl.Should().Be("https://mcp.example.com");
        tool.ServerToken.Should().Be("secret-token");
        tool.AllowedTools.Should().HaveCount(3);
        tool.RequireApproval.Should().Be(ResponderToolApprovalRequirement.Always);
    }

    [Fact]
    public void ResponderToolDefinition_Equality_WorksCorrectly()
    {
        // Arrange
        var tool1 = new ResponderToolDefinition { Type = "mcp", ServerLabel = "server-1" };
        var tool2 = new ResponderToolDefinition { Type = "mcp", ServerLabel = "server-1" };

        // Assert
        tool1.Should().Be(tool2);
    }

    [Theory]
    [InlineData(ResponderToolApprovalRequirement.Never)]
    [InlineData(ResponderToolApprovalRequirement.Always)]
    public void ResponderToolDefinition_AcceptsAllApprovalRequirements(ResponderToolApprovalRequirement approval)
    {
        // Arrange & Act
        var tool = new ResponderToolDefinition
        {
            Type = "mcp",
            RequireApproval = approval
        };

        // Assert
        tool.RequireApproval.Should().Be(approval);
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
        Enum.GetValues<ResponseTextFormat>().Should().Contain(ResponseTextFormat.Text);
        Enum.GetValues<ResponseTextFormat>().Should().Contain(ResponseTextFormat.JsonObject);
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
        Enum.GetValues<ResponderToolApprovalRequirement>().Should().Contain(ResponderToolApprovalRequirement.Never);
        Enum.GetValues<ResponderToolApprovalRequirement>().Should().Contain(ResponderToolApprovalRequirement.Always);
    }

    [Theory]
    [InlineData(ResponderToolApprovalRequirement.Never, 0)]
    [InlineData(ResponderToolApprovalRequirement.Always, 1)]
    public void ResponderToolApprovalRequirement_HasCorrectIntValues(ResponderToolApprovalRequirement requirement, int expectedValue) =>
        // Assert
        ((int)requirement).Should().Be(expectedValue);
}
