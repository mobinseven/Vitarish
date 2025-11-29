namespace Vitarish.Ai.Core.Domain.Models;

using System.Text.Json.Serialization;

/// <summary>
/// Text format for AI responses.
/// </summary>
public enum ResponseTextFormat
{
    /// <summary>Plain text format.</summary>
    Text,
    /// <summary>JSON object format.</summary>
    JsonObject
}

/// <summary>
/// Approval requirement for responder tools.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ResponderToolApprovalRequirement
{
    /// <summary>Never require approval.</summary>
    Never,
    /// <summary>Always require approval.</summary>
    Always
}

/// <summary>
/// Definition of a responder tool.
/// </summary>
public sealed record ResponderToolDefinition
{
    /// <summary>
    /// Gets the tool type.
    /// </summary>
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    /// <summary>
    /// Gets the server label.
    /// </summary>
    [JsonPropertyName("server_label")]
    public string? ServerLabel { get; init; }

    /// <summary>
    /// Gets the server URL.
    /// </summary>
    [JsonPropertyName("server_url")]
    public string? ServerUrl { get; init; }

    /// <summary>
    /// Gets the server token.
    /// </summary>
    [JsonPropertyName("server_token")]
    public string? ServerToken { get; init; }

    /// <summary>
    /// Gets the list of allowed tools.
    /// </summary>
    [JsonPropertyName("allowed_tools")]
    public IReadOnlyList<string> AllowedTools { get; init; } = [];

    /// <summary>
    /// Gets the approval requirement.
    /// </summary>
    [JsonPropertyName("require_approval")]
    public ResponderToolApprovalRequirement RequireApproval { get; init; } = ResponderToolApprovalRequirement.Never;
}

/// <summary>
/// Options for the AI responder.
/// </summary>
public sealed record ResponderOptions
{
    /// <summary>
    /// Gets the system instructions.
    /// </summary>
    [JsonPropertyName("instructions")]
    public string? Instructions { get; init; }

    /// <summary>
    /// Gets the text format for responses.
    /// </summary>
    [JsonPropertyName("text_format")]
    public ResponseTextFormat TextFormat { get; init; } = ResponseTextFormat.Text;

    /// <summary>
    /// Gets the previous response ID for conversation continuity.
    /// </summary>
    [JsonPropertyName("previous_response_id")]
    public string? PreviousResponseId { get; init; }

    /// <summary>
    /// Gets the model to use.
    /// </summary>
    [JsonPropertyName("model")]
    public string? Model { get; init; }

    /// <summary>
    /// Gets the tool definitions.
    /// </summary>
    [JsonPropertyName("tools")]
    public IReadOnlyList<ResponderToolDefinition> Tools { get; init; } = Array.Empty<ResponderToolDefinition>();

    /// <summary>
    /// Gets whether web search tool is enabled.
    /// </summary>
    [JsonPropertyName("enable_web_search_tool")]
    public bool EnableWebSearchTool { get; init; }

    /// <summary>
    /// Gets whether computer tool is enabled.
    /// </summary>
    [JsonPropertyName("enable_computer_tool")]
    public bool EnableComputerTool { get; init; }
}
