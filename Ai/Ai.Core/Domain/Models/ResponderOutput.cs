namespace Vitarish.Ai.Core.Domain.Models;

/// <summary>
/// Type of responder output.
/// </summary>
public enum ResponderOutputType
{
    /// <summary>Text output.</summary>
    Text,
    /// <summary>MCP tool call output.</summary>
    McpToolCall,
    /// <summary>MCP list tools output.</summary>
    McpListTools,
    /// <summary>MCP approval request output.</summary>
    McpApprovalRequest,
    /// <summary>Message output.</summary>
    Message,
    /// <summary>Reasoning output.</summary>
    Reasoning,
    /// <summary>Completion output.</summary>
    Completion
}

/// <summary>
/// Description of an MCP tool.
/// </summary>
/// <param name="Name">The tool name.</param>
/// <param name="Description">Optional tool description.</param>
public sealed record McpToolDescription(string Name, string? Description);

/// <summary>
/// Output from the AI responder.
/// </summary>
/// <param name="Type">The type of output.</param>
/// <param name="Text">Text content.</param>
/// <param name="ToolName">Tool name for tool calls.</param>
/// <param name="ToolArguments">Tool arguments for tool calls.</param>
/// <param name="ServerLabel">Server label for MCP operations.</param>
/// <param name="Id">Output identifier.</param>
/// <param name="Tools">List of tools for MCP list tools output.</param>
/// <param name="Content">Content items for composite outputs.</param>
/// <param name="Metadata">Additional metadata.</param>
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
    /// <summary>
    /// Creates a text output.
    /// </summary>
    /// <param name="text">The text content.</param>
    /// <returns>A new ResponderOutput for text.</returns>
    public static ResponderOutput TextOutput(string text)
        => new(ResponderOutputType.Text, Text: text);

    /// <summary>
    /// Creates an MCP tool call output.
    /// </summary>
    public static ResponderOutput McpToolCallOutput(string toolName, string? toolArguments, string? serverLabel, string? id)
        => new(ResponderOutputType.McpToolCall, ToolName: toolName, ToolArguments: toolArguments, ServerLabel: serverLabel, Id: id);

    /// <summary>
    /// Creates an MCP list tools output.
    /// </summary>
    public static ResponderOutput McpListToolsOutput(string? serverLabel, IReadOnlyList<McpToolDescription> tools)
        => new(ResponderOutputType.McpListTools, ServerLabel: serverLabel, Tools: tools);

    /// <summary>
    /// Creates an MCP approval request output.
    /// </summary>
    public static ResponderOutput McpApprovalRequestOutput(string toolName, string? toolArguments, string? serverLabel, string? id)
        => new(ResponderOutputType.McpApprovalRequest, ToolName: toolName, ToolArguments: toolArguments, ServerLabel: serverLabel, Id: id);

    /// <summary>
    /// Creates a message output.
    /// </summary>
    public static ResponderOutput MessageOutput(IReadOnlyList<ResponderOutput> content)
        => new(ResponderOutputType.Message, Content: content);

    /// <summary>
    /// Creates a reasoning output.
    /// </summary>
    public static ResponderOutput ReasoningOutput(string text)
        => new(ResponderOutputType.Reasoning, Text: text);

    /// <summary>
    /// Creates a completion output.
    /// </summary>
    public static ResponderOutput CompletionOutput(string? responseId, IReadOnlyList<ResponderOutput> content, IReadOnlyDictionary<string, string>? metadata = null)
        => new(ResponderOutputType.Completion, Id: responseId, Content: content, Metadata: metadata);
}
