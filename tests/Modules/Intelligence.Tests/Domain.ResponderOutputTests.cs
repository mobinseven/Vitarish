namespace Vitarish.Intelligence.Tests;

/// <summary>
/// Unit tests for the <see cref="ResponderOutput"/> record.
/// </summary>
public sealed class ResponderOutputTests
{
    [Fact]
    public void TextOutput_CreatesTextTypeOutput()
    {
        // Arrange & Act
        var output = ResponderOutput.TextOutput("Hello, world!");

        // Assert
        output.Type.Should().Be(ResponderOutputType.Text);
        output.Text.Should().Be("Hello, world!");
        output.ToolName.Should().BeNull();
        output.ToolArguments.Should().BeNull();
        output.ServerLabel.Should().BeNull();
        output.Id.Should().BeNull();
        output.Tools.Should().BeNull();
        output.Content.Should().BeNull();
        output.Metadata.Should().BeNull();
    }

    [Fact]
    public void McpToolCallOutput_CreatesMcpToolCallTypeOutput()
    {
        // Arrange & Act
        var output = ResponderOutput.McpToolCallOutput(
            toolName: "search",
            toolArguments: """{"query": "test"}""",
            serverLabel: "web-search",
            id: "call-123");

        // Assert
        output.Type.Should().Be(ResponderOutputType.McpToolCall);
        output.ToolName.Should().Be("search");
        output.ToolArguments.Should().Be("""{"query": "test"}""");
        output.ServerLabel.Should().Be("web-search");
        output.Id.Should().Be("call-123");
    }

    [Fact]
    public void McpListToolsOutput_CreatesMcpListToolsTypeOutput()
    {
        // Arrange
        var tools = new List<McpToolDescription>
        {
            new("search", "Search the web"),
            new("calculate", "Perform calculations")
        };

        // Act
        var output = ResponderOutput.McpListToolsOutput("web-server", tools);

        // Assert
        output.Type.Should().Be(ResponderOutputType.McpListTools);
        output.ServerLabel.Should().Be("web-server");
        output.Tools.Should().HaveCount(2);
        output.Tools![0].Name.Should().Be("search");
        output.Tools[1].Name.Should().Be("calculate");
    }

    [Fact]
    public void McpApprovalRequestOutput_CreatesMcpApprovalRequestTypeOutput()
    {
        // Arrange & Act
        var output = ResponderOutput.McpApprovalRequestOutput(
            toolName: "delete_file",
            toolArguments: """{"path": "/data/file.txt"}""",
            serverLabel: "file-manager",
            id: "approval-456");

        // Assert
        output.Type.Should().Be(ResponderOutputType.McpApprovalRequest);
        output.ToolName.Should().Be("delete_file");
        output.ToolArguments.Should().Be("""{"path": "/data/file.txt"}""");
        output.ServerLabel.Should().Be("file-manager");
        output.Id.Should().Be("approval-456");
    }

    [Fact]
    public void MessageOutput_CreatesMessageTypeOutput()
    {
        // Arrange
        var content = new List<ResponderOutput>
        {
            ResponderOutput.TextOutput("Part 1"),
            ResponderOutput.TextOutput("Part 2")
        };

        // Act
        var output = ResponderOutput.MessageOutput(content);

        // Assert
        output.Type.Should().Be(ResponderOutputType.Message);
        output.Content.Should().HaveCount(2);
        output.Content![0].Text.Should().Be("Part 1");
        output.Content[1].Text.Should().Be("Part 2");
    }

    [Fact]
    public void ReasoningOutput_CreatesReasoningTypeOutput()
    {
        // Arrange & Act
        var output = ResponderOutput.ReasoningOutput("Let me think about this...");

        // Assert
        output.Type.Should().Be(ResponderOutputType.Reasoning);
        output.Text.Should().Be("Let me think about this...");
    }

    [Fact]
    public void CompletionOutput_CreatesCompletionTypeOutput()
    {
        // Arrange
        var content = new List<ResponderOutput>
        {
            ResponderOutput.TextOutput("Final response")
        };
        var metadata = new Dictionary<string, string>
        {
            ["tokens"] = "100",
            ["model"] = "gpt-4"
        };

        // Act
        var output = ResponderOutput.CompletionOutput("response-789", content, metadata);

        // Assert
        output.Type.Should().Be(ResponderOutputType.Completion);
        output.Id.Should().Be("response-789");
        output.Content.Should().HaveCount(1);
        output.Metadata.Should().HaveCount(2);
        output.Metadata!["tokens"].Should().Be("100");
    }

    [Fact]
    public void CompletionOutput_WithoutMetadata_CreatesCompletionTypeOutput()
    {
        // Arrange
        var content = new List<ResponderOutput>
        {
            ResponderOutput.TextOutput("Response")
        };

        // Act
        var output = ResponderOutput.CompletionOutput("response-123", content);

        // Assert
        output.Type.Should().Be(ResponderOutputType.Completion);
        output.Id.Should().Be("response-123");
        output.Metadata.Should().BeNull();
    }

    [Fact]
    public void ResponderOutput_Equality_WorksCorrectly()
    {
        // Arrange
        var output1 = ResponderOutput.TextOutput("Hello");
        var output2 = ResponderOutput.TextOutput("Hello");

        // Assert
        output1.Should().Be(output2);
    }

    [Fact]
    public void ResponderOutput_Inequality_WhenTypeDiffers()
    {
        // Arrange
        var output1 = ResponderOutput.TextOutput("test");
        var output2 = ResponderOutput.ReasoningOutput("test");

        // Assert
        output1.Should().NotBe(output2);
    }

    [Fact]
    public void ResponderOutput_Inequality_WhenTextDiffers()
    {
        // Arrange
        var output1 = ResponderOutput.TextOutput("Hello");
        var output2 = ResponderOutput.TextOutput("World");

        // Assert
        output1.Should().NotBe(output2);
    }

    [Fact]
    public void ResponderOutput_WithCopy_CreatesNewInstance()
    {
        // Arrange
        var original = ResponderOutput.TextOutput("Original");

        // Act
        var copy = original with { Text = "Modified" };

        // Assert
        copy.Text.Should().Be("Modified");
        original.Text.Should().Be("Original");
    }

    [Theory]
    [InlineData("")]
    [InlineData("Short response")]
    [InlineData("A much longer response that contains detailed information and explanations.")]
    public void TextOutput_AcceptsVariousTextLengths(string text)
    {
        // Arrange & Act
        var output = ResponderOutput.TextOutput(text);

        // Assert
        output.Text.Should().Be(text);
    }

    [Fact]
    public void McpToolCallOutput_WithNullArguments_SetsArgumentsToNull()
    {
        // Arrange & Act
        var output = ResponderOutput.McpToolCallOutput("get_time", null, "time-server", "call-1");

        // Assert
        output.ToolName.Should().Be("get_time");
        output.ToolArguments.Should().BeNull();
    }

    [Fact]
    public void McpListToolsOutput_WithEmptyToolsList_SetsEmptyList()
    {
        // Arrange & Act
        var output = ResponderOutput.McpListToolsOutput("server", []);

        // Assert
        output.Tools.Should().BeEmpty();
    }

    [Fact]
    public void MessageOutput_WithEmptyContent_SetsEmptyList()
    {
        // Arrange & Act
        var output = ResponderOutput.MessageOutput([]);

        // Assert
        output.Content.Should().BeEmpty();
    }
}

/// <summary>
/// Unit tests for the <see cref="ResponderOutputType"/> enum.
/// </summary>
public sealed class ResponderOutputTypeTests
{
    [Fact]
    public void ResponderOutputType_HasExpectedValues()
    {
        // Assert
        Enum.GetValues<ResponderOutputType>().Should().Contain(ResponderOutputType.Text);
        Enum.GetValues<ResponderOutputType>().Should().Contain(ResponderOutputType.McpToolCall);
        Enum.GetValues<ResponderOutputType>().Should().Contain(ResponderOutputType.McpListTools);
        Enum.GetValues<ResponderOutputType>().Should().Contain(ResponderOutputType.McpApprovalRequest);
        Enum.GetValues<ResponderOutputType>().Should().Contain(ResponderOutputType.Message);
        Enum.GetValues<ResponderOutputType>().Should().Contain(ResponderOutputType.Reasoning);
        Enum.GetValues<ResponderOutputType>().Should().Contain(ResponderOutputType.Completion);
    }

    [Theory]
    [InlineData(ResponderOutputType.Text, 0)]
    [InlineData(ResponderOutputType.McpToolCall, 1)]
    [InlineData(ResponderOutputType.McpListTools, 2)]
    [InlineData(ResponderOutputType.McpApprovalRequest, 3)]
    [InlineData(ResponderOutputType.Message, 4)]
    [InlineData(ResponderOutputType.Reasoning, 5)]
    [InlineData(ResponderOutputType.Completion, 6)]
    public void ResponderOutputType_HasCorrectIntValues(ResponderOutputType type, int expectedValue)
    {
        // Assert
        ((int)type).Should().Be(expectedValue);
    }
}

/// <summary>
/// Unit tests for the <see cref="McpToolDescription"/> record.
/// </summary>
public sealed class McpToolDescriptionTests
{
    [Fact]
    public void McpToolDescription_WithNameAndDescription_CreatesValidInstance()
    {
        // Arrange & Act
        var tool = new McpToolDescription("search", "Search the web for information");

        // Assert
        tool.Name.Should().Be("search");
        tool.Description.Should().Be("Search the web for information");
    }

    [Fact]
    public void McpToolDescription_WithNullDescription_CreatesValidInstance()
    {
        // Arrange & Act
        var tool = new McpToolDescription("tool-name", null);

        // Assert
        tool.Name.Should().Be("tool-name");
        tool.Description.Should().BeNull();
    }

    [Fact]
    public void McpToolDescription_Equality_WorksCorrectly()
    {
        // Arrange
        var tool1 = new McpToolDescription("search", "Search description");
        var tool2 = new McpToolDescription("search", "Search description");

        // Assert
        tool1.Should().Be(tool2);
    }

    [Fact]
    public void McpToolDescription_Inequality_WhenNameDiffers()
    {
        // Arrange
        var tool1 = new McpToolDescription("search", "Description");
        var tool2 = new McpToolDescription("calculate", "Description");

        // Assert
        tool1.Should().NotBe(tool2);
    }

    [Fact]
    public void McpToolDescription_Inequality_WhenDescriptionDiffers()
    {
        // Arrange
        var tool1 = new McpToolDescription("search", "Search v1");
        var tool2 = new McpToolDescription("search", "Search v2");

        // Assert
        tool1.Should().NotBe(tool2);
    }

    [Theory]
    [InlineData("get_weather")]
    [InlineData("calculate_sum")]
    [InlineData("send_email")]
    [InlineData("file_read")]
    public void McpToolDescription_AcceptsVariousToolNames(string name)
    {
        // Arrange & Act
        var tool = new McpToolDescription(name, "Test description");

        // Assert
        tool.Name.Should().Be(name);
    }
}
