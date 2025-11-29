namespace Vitarish.Ai.Core.Tests.Domain.Models;

using Vitarish.Ai.Core.Domain.Models;

public class ResponderInputTests
{
    [Fact]
    public void TextInput_CreatesTextTypeInput()
    {
        // Arrange & Act
        var input = ResponderInput.TextInput("Hello AI");

        // Assert
        Assert.Equal(ResponderInputType.Text, input.Type);
        Assert.Equal("Hello AI", input.Text);
        Assert.Null(input.ImageUrl);
        Assert.Null(input.FileId);
        Assert.Null(input.ApprovalId);
    }

    [Fact]
    public void ImageInput_CreatesImageTypeInput()
    {
        // Arrange & Act
        var input = ResponderInput.ImageInput("https://example.com/image.jpg", ResponderInputImageDetail.High);

        // Assert
        Assert.Equal(ResponderInputType.Image, input.Type);
        Assert.Equal("https://example.com/image.jpg", input.ImageUrl);
        Assert.Equal(ResponderInputImageDetail.High, input.Detail);
        Assert.Null(input.Text);
    }

    [Fact]
    public void FileInput_CreatesFileTypeInput()
    {
        // Arrange & Act
        var input = ResponderInput.FileInput(fileId: "file-123", fileData: "base64data", filename: "doc.pdf");

        // Assert
        Assert.Equal(ResponderInputType.File, input.Type);
        Assert.Equal("file-123", input.FileId);
        Assert.Equal("base64data", input.FileData);
        Assert.Equal("doc.pdf", input.Filename);
    }

    [Fact]
    public void McpToolCallApprovalInput_CreatesMcpApprovalTypeInput()
    {
        // Arrange & Act
        var input = ResponderInput.McpToolCallApprovalInput("approval-456");

        // Assert
        Assert.Equal(ResponderInputType.McpToolCallApproval, input.Type);
        Assert.Equal("approval-456", input.ApprovalId);
    }
}

public class ResponderOutputTests
{
    [Fact]
    public void TextOutput_CreatesTextTypeOutput()
    {
        // Arrange & Act
        var output = ResponderOutput.TextOutput("AI response");

        // Assert
        Assert.Equal(ResponderOutputType.Text, output.Type);
        Assert.Equal("AI response", output.Text);
    }

    [Fact]
    public void McpToolCallOutput_CreatesMcpToolCallTypeOutput()
    {
        // Arrange & Act
        var output = ResponderOutput.McpToolCallOutput("toolName", "{\"arg\":1}", "server1", "call-123");

        // Assert
        Assert.Equal(ResponderOutputType.McpToolCall, output.Type);
        Assert.Equal("toolName", output.ToolName);
        Assert.Equal("{\"arg\":1}", output.ToolArguments);
        Assert.Equal("server1", output.ServerLabel);
        Assert.Equal("call-123", output.Id);
    }

    [Fact]
    public void McpListToolsOutput_CreatesMcpListToolsTypeOutput()
    {
        // Arrange
        var tools = new List<McpToolDescription>
        {
            new("tool1", "Description 1"),
            new("tool2", null)
        };

        // Act
        var output = ResponderOutput.McpListToolsOutput("server2", tools);

        // Assert
        Assert.Equal(ResponderOutputType.McpListTools, output.Type);
        Assert.Equal("server2", output.ServerLabel);
        Assert.Equal(2, output.Tools?.Count);
        Assert.Equal("tool1", output.Tools?[0].Name);
    }

    [Fact]
    public void McpApprovalRequestOutput_CreatesMcpApprovalRequestTypeOutput()
    {
        // Arrange & Act
        var output = ResponderOutput.McpApprovalRequestOutput("dangerousTool", "{}", "srv", "req-1");

        // Assert
        Assert.Equal(ResponderOutputType.McpApprovalRequest, output.Type);
        Assert.Equal("dangerousTool", output.ToolName);
        Assert.Equal("{}", output.ToolArguments);
        Assert.Equal("srv", output.ServerLabel);
        Assert.Equal("req-1", output.Id);
    }

    [Fact]
    public void MessageOutput_CreatesMessageTypeOutput()
    {
        // Arrange
        var content = new List<ResponderOutput> { ResponderOutput.TextOutput("Part 1") };

        // Act
        var output = ResponderOutput.MessageOutput(content);

        // Assert
        Assert.Equal(ResponderOutputType.Message, output.Type);
        Assert.Single(output.Content!);
    }

    [Fact]
    public void ReasoningOutput_CreatesReasoningTypeOutput()
    {
        // Arrange & Act
        var output = ResponderOutput.ReasoningOutput("Thinking...");

        // Assert
        Assert.Equal(ResponderOutputType.Reasoning, output.Type);
        Assert.Equal("Thinking...", output.Text);
    }

    [Fact]
    public void CompletionOutput_CreatesCompletionTypeOutput()
    {
        // Arrange
        var content = new List<ResponderOutput> { ResponderOutput.TextOutput("Final answer") };
        var metadata = new Dictionary<string, string> { ["tokens"] = "100" };

        // Act
        var output = ResponderOutput.CompletionOutput("resp-123", content, metadata);

        // Assert
        Assert.Equal(ResponderOutputType.Completion, output.Type);
        Assert.Equal("resp-123", output.Id);
        Assert.Single(output.Content!);
        Assert.Equal("100", output.Metadata?["tokens"]);
    }
}

public class McpToolDescriptionTests
{
    [Fact]
    public void McpToolDescription_Constructor_SetsProperties()
    {
        // Arrange & Act
        var tool = new McpToolDescription("calculator", "Performs calculations");

        // Assert
        Assert.Equal("calculator", tool.Name);
        Assert.Equal("Performs calculations", tool.Description);
    }

    [Fact]
    public void McpToolDescription_NullDescription_IsValid()
    {
        // Arrange & Act
        var tool = new McpToolDescription("simple_tool", null);

        // Assert
        Assert.Equal("simple_tool", tool.Name);
        Assert.Null(tool.Description);
    }
}
