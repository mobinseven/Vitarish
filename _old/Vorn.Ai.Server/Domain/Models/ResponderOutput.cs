namespace Vorn.Ai.Server.Domain.Models;

public enum ResponderOutputType
{
    Text,
    McpToolCall,
    McpListTools,
    McpApprovalRequest,
    Message,
    Reasoning,
    Completion
}

public sealed record ResponderOutput(
    ResponderOutputType Type,
    string? Text = null,
    string? ToolName = null,
    string? ToolArguments = null,
    string? ServerLabel = null,
    string? Id = null,
    IReadOnlyList<McpToolDescription>? Tools = null,
    IReadOnlyList<ResponderOutput>? Content = null,
    IReadOnlyDictionary<string, string>? Metadata = null
)
{
    public static ResponderOutput TextOutput(string text)
    {
        return new(ResponderOutputType.Text, Text: text);
    }

    public static ResponderOutput McpToolCallOutput(string toolName, string? toolArguments, string? serverLabel, string? id)
    {
        return new(ResponderOutputType.McpToolCall, ToolName: toolName, ToolArguments: toolArguments, ServerLabel: serverLabel, Id: id);
    }

    public static ResponderOutput McpListToolsOutput(string? serverLabel, IReadOnlyList<McpToolDescription> tools)
    {
        return new(ResponderOutputType.McpListTools, ServerLabel: serverLabel, Tools: tools);
    }

    public static ResponderOutput McpApprovalRequestOutput(string toolName, string? toolArguments, string? serverLabel, string? id)
    {
        return new(ResponderOutputType.McpApprovalRequest, ToolName: toolName, ToolArguments: toolArguments, ServerLabel: serverLabel, Id: id);
    }

    public static ResponderOutput MessageOutput(IReadOnlyList<ResponderOutput> content)
    {
        return new(ResponderOutputType.Message, Content: content);
    }

    public static ResponderOutput ReasoningOutput(string text)
    {
        return new(ResponderOutputType.Reasoning, Text: text);
    }

    public static ResponderOutput CompletionOutput(string? responseId, IReadOnlyList<ResponderOutput> content, IReadOnlyDictionary<string, string>? metadata = null)
    {
        return new(ResponderOutputType.Completion, Id: responseId, Content: content, Metadata: metadata);
    }
}

public sealed record McpToolDescription(string Name, string? Description);
