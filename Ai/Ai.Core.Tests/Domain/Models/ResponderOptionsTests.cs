namespace Vitarish.Ai.Core.Tests.Domain.Models;

using Vitarish.Ai.Core.Domain.Models;

public class ResponderOptionsTests
{
    [Fact]
    public void ResponderOptions_NewInstance_HasDefaultValues()
    {
        // Arrange & Act
        var options = new ResponderOptions();

        // Assert
        Assert.Null(options.Instructions);
        Assert.Equal(ResponseTextFormat.Text, options.TextFormat);
        Assert.Null(options.PreviousResponseId);
        Assert.Null(options.Model);
        Assert.Empty(options.Tools);
        Assert.False(options.EnableWebSearchTool);
        Assert.False(options.EnableComputerTool);
    }

    [Fact]
    public void ResponderOptions_WithAllProperties_ValuesAreCorrect()
    {
        // Arrange
        var tools = new List<ResponderToolDefinition>
        {
            new() { Type = "mcp", ServerLabel = "srv1" }
        };

        // Act
        var options = new ResponderOptions
        {
            Instructions = "Be helpful",
            TextFormat = ResponseTextFormat.JsonObject,
            PreviousResponseId = "prev-123",
            Model = "gpt-4",
            Tools = tools,
            EnableWebSearchTool = true,
            EnableComputerTool = true
        };

        // Assert
        Assert.Equal("Be helpful", options.Instructions);
        Assert.Equal(ResponseTextFormat.JsonObject, options.TextFormat);
        Assert.Equal("prev-123", options.PreviousResponseId);
        Assert.Equal("gpt-4", options.Model);
        Assert.Single(options.Tools);
        Assert.True(options.EnableWebSearchTool);
        Assert.True(options.EnableComputerTool);
    }
}

public class ResponderToolDefinitionTests
{
    [Fact]
    public void ResponderToolDefinition_NewInstance_HasDefaultValues()
    {
        // Arrange & Act
        var tool = new ResponderToolDefinition { Type = "function" };

        // Assert
        Assert.Equal("function", tool.Type);
        Assert.Null(tool.ServerLabel);
        Assert.Null(tool.ServerUrl);
        Assert.Null(tool.ServerToken);
        Assert.Empty(tool.AllowedTools);
        Assert.Equal(ResponderToolApprovalRequirement.Never, tool.RequireApproval);
    }

    [Fact]
    public void ResponderToolDefinition_WithAllProperties_ValuesAreCorrect()
    {
        // Arrange & Act
        var tool = new ResponderToolDefinition
        {
            Type = "mcp",
            ServerLabel = "my-server",
            ServerUrl = "https://mcp.example.com",
            ServerToken = "secret-token",
            AllowedTools = ["tool1", "tool2"],
            RequireApproval = ResponderToolApprovalRequirement.Always
        };

        // Assert
        Assert.Equal("mcp", tool.Type);
        Assert.Equal("my-server", tool.ServerLabel);
        Assert.Equal("https://mcp.example.com", tool.ServerUrl);
        Assert.Equal("secret-token", tool.ServerToken);
        Assert.Equal(2, tool.AllowedTools.Count);
        Assert.Contains("tool1", tool.AllowedTools);
        Assert.Equal(ResponderToolApprovalRequirement.Always, tool.RequireApproval);
    }
}

public class ResponseTextFormatTests
{
    [Fact]
    public void ResponseTextFormat_HasExpectedValues()
    {
        // Assert
        Assert.Equal(0, (int)ResponseTextFormat.Text);
        Assert.Equal(1, (int)ResponseTextFormat.JsonObject);
    }
}

public class ResponderToolApprovalRequirementTests
{
    [Fact]
    public void ResponderToolApprovalRequirement_HasExpectedValues()
    {
        // Assert
        Assert.Equal(0, (int)ResponderToolApprovalRequirement.Never);
        Assert.Equal(1, (int)ResponderToolApprovalRequirement.Always);
    }
}
