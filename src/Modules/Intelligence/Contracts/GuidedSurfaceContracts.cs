using System.Text.Json.Serialization;

namespace Vitarish.Intelligence.Contracts;

/// <summary>The platform block kinds of a guided surface (ADR-0022).</summary>
[JsonConverter(typeof(JsonStringEnumConverter<GuidedSurfaceBlockKindResponse>))]
public enum GuidedSurfaceBlockKindResponse
{
    Question,
    NextAction,
    ActionGroup
}

/// <summary>One predesigned component of a reply; exactly the member matching <see cref="Kind"/> is set.</summary>
public sealed record GuidedSurfaceBlockResponse(
    GuidedSurfaceBlockKindResponse Kind,
    GuidedSurfaceQuestionResponse? Question = null,
    string? NextAction = null,
    GuidedSurfaceActionGroupResponse? ActionGroup = null);

/// <summary>A question the model asked, with its validated prompt and option labels.</summary>
public sealed record GuidedSurfaceQuestionResponse(
    string Prompt,
    IReadOnlyList<GuidedSurfaceOptionResponse> Options,
    int MaxSelections);

public sealed record GuidedSurfaceOptionResponse(string Key, string Label);

public sealed record GuidedSurfaceActionGroupResponse(string AgentRunId, IReadOnlyList<Guid> ActionIds);
