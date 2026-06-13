using System.Text.Json.Serialization;

namespace Vitarish.Intelligence.Domain.Models;

public sealed record ResponderOptions
{
    [JsonPropertyName("instructions")]
    public string? Instructions { get; init; }

    [JsonPropertyName("text_format")]
    public ResponseTextFormat TextFormat { get; init; } = ResponseTextFormat.Text;

    [JsonPropertyName("json_schema")]
    public ResponderJsonSchema? JsonSchema { get; init; }

    [JsonPropertyName("previous_response_id")]
    public string? PreviousResponseId { get; init; }

    [JsonPropertyName("model")]
    public string? Model { get; init; }

    [JsonPropertyName("tools")]
    public IReadOnlyList<ResponderToolDefinition> Tools { get; init; } = Array.Empty<ResponderToolDefinition>();

    [JsonPropertyName("enable_web_search_tool")]
    public bool EnableWebSearchTool { get; init; }

    [JsonPropertyName("enable_computer_tool")]
    public bool EnableComputerTool { get; init; }
}

public sealed record ResponderJsonSchema
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("schema")]
    public required string Schema { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("strict")]
    public bool Strict { get; init; } = true;
}

public sealed record ResponderToolDefinition
{
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("server_label")]
    public string? ServerLabel { get; init; }

    [JsonPropertyName("server_url")]
    public string? ServerUrl { get; init; }

    [JsonPropertyName("server_token")]
    public string? ServerToken { get; init; }

    [JsonPropertyName("allowed_tools")]
    public IReadOnlyList<string> AllowedTools { get; init; } = [];

    [JsonPropertyName("require_approval")]
    public ResponderToolApprovalRequirement RequireApproval { get; init; } = ResponderToolApprovalRequirement.Never;
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ResponderToolApprovalRequirement
{
    Never,
    Always
}

public enum ResponseTextFormat
{
    Text,
    JsonObject,
    JsonSchema
}
