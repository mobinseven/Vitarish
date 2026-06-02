namespace Intelligence.Tests;

/// <summary>
/// Unit tests for the <see cref="ResponderOutput"/> record.
/// </summary>
public sealed class ResponderOutputTests
{
    [Fact]
    public void TextOutput_CreatesTextTypeOutput()
    {
        // Arrange & Act
        ResponderOutput output = ResponderOutput.TextOutput("Hello, world!");

        // Assert
        _ = output.Type.Should().Be(ResponderOutputType.Text);
        _ = output.Text.Should().Be("Hello, world!");
        _ = output.ToolName.Should().BeNull();
        _ = output.ToolArguments.Should().BeNull();
        _ = output.ServerLabel.Should().BeNull();
        _ = output.Id.Should().BeNull();
        _ = output.Tools.Should().BeNull();
        _ = output.Content.Should().BeNull();
        _ = output.Metadata.Should().BeNull();
    }

    [Fact]
    public void McpToolCallOutput_CreatesMcpToolCallTypeOutput()
    {
        // Arrange & Act
        ResponderOutput output = ResponderOutput.McpToolCallOutput(
            toolName: "search",
            toolArguments: """{"query": "test"}""",
            serverLabel: "web-search",
            id: "call-123");

        // Assert
        _ = output.Type.Should().Be(ResponderOutputType.McpToolCall);
        _ = output.ToolName.Should().Be("search");
        _ = output.ToolArguments.Should().Be("""{"query": "test"}""");
        _ = output.ServerLabel.Should().Be("web-search");
        _ = output.Id.Should().Be("call-123");
    }

    [Fact]
    public void McpListToolsOutput_CreatesMcpListToolsTypeOutput()
    {
        // Arrange
        List<McpToolDescription> tools =
        [
            new("search", "Search the web"),
            new("calculate", "Perform calculations")
        ];

        // Act
        ResponderOutput output = ResponderOutput.McpListToolsOutput("web-server", tools);

        // Assert
        _ = output.Type.Should().Be(ResponderOutputType.McpListTools);
        _ = output.ServerLabel.Should().Be("web-server");
        _ = output.Tools.Should().HaveCount(2);
        _ = output.Tools![0].Name.Should().Be("search");
        _ = output.Tools[1].Name.Should().Be("calculate");
    }

    [Fact]
    public void McpApprovalRequestOutput_CreatesMcpApprovalRequestTypeOutput()
    {
        // Arrange & Act
        ResponderOutput output = ResponderOutput.McpApprovalRequestOutput(
            toolName: "delete_file",
            toolArguments: """{"path": "/data/file.txt"}""",
            serverLabel: "file-manager",
            id: "approval-456");

        // Assert
        _ = output.Type.Should().Be(ResponderOutputType.McpApprovalRequest);
        _ = output.ToolName.Should().Be("delete_file");
        _ = output.ToolArguments.Should().Be("""{"path": "/data/file.txt"}""");
        _ = output.ServerLabel.Should().Be("file-manager");
        _ = output.Id.Should().Be("approval-456");
    }

    [Fact]
    public void MessageOutput_CreatesMessageTypeOutput()
    {
        // Arrange
        List<ResponderOutput> content =
        [
            ResponderOutput.TextOutput("Part 1"),
            ResponderOutput.TextOutput("Part 2")
        ];

        // Act
        ResponderOutput output = ResponderOutput.MessageOutput(content);

        // Assert
        _ = output.Type.Should().Be(ResponderOutputType.Message);
        _ = output.Content.Should().HaveCount(2);
        _ = output.Content![0].Text.Should().Be("Part 1");
        _ = output.Content[1].Text.Should().Be("Part 2");
    }

    [Fact]
    public void ReasoningOutput_CreatesReasoningTypeOutput()
    {
        // Arrange & Act
        ResponderOutput output = ResponderOutput.ReasoningOutput("Let me think about this...");

        // Assert
        _ = output.Type.Should().Be(ResponderOutputType.Reasoning);
        _ = output.Text.Should().Be("Let me think about this...");
    }

    [Fact]
    public void CompletionOutput_CreatesCompletionTypeOutput()
    {
        // Arrange
        List<ResponderOutput> content =
        [
            ResponderOutput.TextOutput("Final response")
        ];
        Dictionary<string, string> metadata = new()
        {
            ["tokens"] = "100",
            ["model"] = "gpt-4"
        };

        // Act
        ResponderOutput output = ResponderOutput.CompletionOutput("response-789", content, metadata);

        // Assert
        _ = output.Type.Should().Be(ResponderOutputType.Completion);
        _ = output.Id.Should().Be("response-789");
        _ = output.Content.Should().HaveCount(1);
        _ = output.Metadata.Should().HaveCount(2);
        _ = output.Metadata!["tokens"].Should().Be("100");
    }

    [Fact]
    public void CompletionOutput_WithoutMetadata_CreatesCompletionTypeOutput()
    {
        // Arrange
        List<ResponderOutput> content =
        [
            ResponderOutput.TextOutput("Response")
        ];

        // Act
        ResponderOutput output = ResponderOutput.CompletionOutput("response-123", content);

        // Assert
        _ = output.Type.Should().Be(ResponderOutputType.Completion);
        _ = output.Id.Should().Be("response-123");
        _ = output.Metadata.Should().BeNull();
    }

    [Fact]
    public void ResponderOutput_Equality_WorksCorrectly()
    {
        // Arrange
        ResponderOutput output1 = ResponderOutput.TextOutput("Hello");
        ResponderOutput output2 = ResponderOutput.TextOutput("Hello");

        // Assert
        _ = output1.Should().Be(output2);
    }

    [Fact]
    public void ResponderOutput_Inequality_WhenTypeDiffers()
    {
        // Arrange
        ResponderOutput output1 = ResponderOutput.TextOutput("test");
        ResponderOutput output2 = ResponderOutput.ReasoningOutput("test");

        // Assert
        _ = output1.Should().NotBe(output2);
    }

    [Fact]
    public void ResponderOutput_Inequality_WhenTextDiffers()
    {
        // Arrange
        ResponderOutput output1 = ResponderOutput.TextOutput("Hello");
        ResponderOutput output2 = ResponderOutput.TextOutput("World");

        // Assert
        _ = output1.Should().NotBe(output2);
    }

    [Fact]
    public void ResponderOutput_WithCopy_CreatesNewInstance()
    {
        // Arrange
        ResponderOutput original = ResponderOutput.TextOutput("Original");

        // Act
        ResponderOutput copy = original with { Text = "Modified" };

        // Assert
        _ = copy.Text.Should().Be("Modified");
        _ = original.Text.Should().Be("Original");
    }

    [Theory]
    [InlineData("")]
    [InlineData("Short response")]
    [InlineData("A much longer response that contains detailed information and explanations.")]
    public void TextOutput_AcceptsVariousTextLengths(string text)
    {
        // Arrange & Act
        ResponderOutput output = ResponderOutput.TextOutput(text);

        // Assert
        _ = output.Text.Should().Be(text);
    }

    [Fact]
    public void McpToolCallOutput_WithNullArguments_SetsArgumentsToNull()
    {
        // Arrange & Act
        ResponderOutput output = ResponderOutput.McpToolCallOutput("get_time", null, "time-server", "call-1");

        // Assert
        _ = output.ToolName.Should().Be("get_time");
        _ = output.ToolArguments.Should().BeNull();
    }

    [Fact]
    public void McpListToolsOutput_WithEmptyToolsList_SetsEmptyList()
    {
        // Arrange & Act
        ResponderOutput output = ResponderOutput.McpListToolsOutput("server", []);

        // Assert
        _ = output.Tools.Should().BeEmpty();
    }

    [Fact]
    public void MessageOutput_WithEmptyContent_SetsEmptyList()
    {
        // Arrange & Act
        ResponderOutput output = ResponderOutput.MessageOutput([]);

        // Assert
        _ = output.Content.Should().BeEmpty();
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
        _ = Enum.GetValues<ResponderOutputType>().Should().Contain(ResponderOutputType.Text);
        _ = Enum.GetValues<ResponderOutputType>().Should().Contain(ResponderOutputType.McpToolCall);
        _ = Enum.GetValues<ResponderOutputType>().Should().Contain(ResponderOutputType.McpListTools);
        _ = Enum.GetValues<ResponderOutputType>().Should().Contain(ResponderOutputType.McpApprovalRequest);
        _ = Enum.GetValues<ResponderOutputType>().Should().Contain(ResponderOutputType.Message);
        _ = Enum.GetValues<ResponderOutputType>().Should().Contain(ResponderOutputType.Reasoning);
        _ = Enum.GetValues<ResponderOutputType>().Should().Contain(ResponderOutputType.Completion);
    }

    [Theory]
    [InlineData(ResponderOutputType.Text, 0)]
    [InlineData(ResponderOutputType.McpToolCall, 1)]
    [InlineData(ResponderOutputType.McpListTools, 2)]
    [InlineData(ResponderOutputType.McpApprovalRequest, 3)]
    [InlineData(ResponderOutputType.Message, 4)]
    [InlineData(ResponderOutputType.Reasoning, 5)]
    [InlineData(ResponderOutputType.Completion, 6)]
    public void ResponderOutputType_HasCorrectIntValues(ResponderOutputType type, int expectedValue) =>
        // Assert
        ((int)type).Should().Be(expectedValue);
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
        McpToolDescription tool = new("search", "Search the web for information");

        // Assert
        _ = tool.Name.Should().Be("search");
        _ = tool.Description.Should().Be("Search the web for information");
    }

    [Fact]
    public void McpToolDescription_WithNullDescription_CreatesValidInstance()
    {
        // Arrange & Act
        McpToolDescription tool = new("tool-name", null);

        // Assert
        _ = tool.Name.Should().Be("tool-name");
        _ = tool.Description.Should().BeNull();
    }

    [Fact]
    public void McpToolDescription_Equality_WorksCorrectly()
    {
        // Arrange
        McpToolDescription tool1 = new("search", "Search description");
        McpToolDescription tool2 = new("search", "Search description");

        // Assert
        _ = tool1.Should().Be(tool2);
    }

    [Fact]
    public void McpToolDescription_Inequality_WhenNameDiffers()
    {
        // Arrange
        McpToolDescription tool1 = new("search", "Description");
        McpToolDescription tool2 = new("calculate", "Description");

        // Assert
        _ = tool1.Should().NotBe(tool2);
    }

    [Fact]
    public void McpToolDescription_Inequality_WhenDescriptionDiffers()
    {
        // Arrange
        McpToolDescription tool1 = new("search", "Search v1");
        McpToolDescription tool2 = new("search", "Search v2");

        // Assert
        _ = tool1.Should().NotBe(tool2);
    }

    [Theory]
    [InlineData("get_weather")]
    [InlineData("calculate_sum")]
    [InlineData("send_email")]
    [InlineData("file_read")]
    public void McpToolDescription_AcceptsVariousToolNames(string name)
    {
        // Arrange & Act
        McpToolDescription tool = new(name, "Test description");

        // Assert
        _ = tool.Name.Should().Be(name);
    }
}
