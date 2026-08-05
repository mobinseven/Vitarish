using System.Text.Json.Serialization;

namespace Vitarish.Intelligence.Application.Agents;

public sealed record AgentDefinition(
    string Model,
    string Instructions,
    AgentResultDefinition? Result = null,
    [property: JsonConverter(typeof(JsonStringEnumConverter))]
    AgentReasoningLevel ReasoningLevel = AgentReasoningLevel.Low);

public enum AgentReasoningLevel
{
    [JsonStringEnumMemberName("none")]
    None,
    [JsonStringEnumMemberName("minimal")]
    Minimal,
    [JsonStringEnumMemberName("low")]
    Low,
    [JsonStringEnumMemberName("medium")]
    Medium,
    [JsonStringEnumMemberName("high")]
    High,
    [JsonStringEnumMemberName("xhigh")]
    XHigh,
    [JsonStringEnumMemberName("max")]
    Max
}

public sealed record AgentResultDefinition(
    string Name,
    string JsonSchema,
    bool Strict = true);
