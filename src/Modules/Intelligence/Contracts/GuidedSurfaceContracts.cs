using System.Text.Json;
using System.Text.Json.Serialization;

namespace Vitarish.Intelligence.Contracts;

/// <summary>The platform block kinds of a guided surface (ADR-0022).</summary>
[JsonConverter(typeof(JsonStringEnumConverter<GuidedSurfaceBlockKindResponse>))]
public enum GuidedSurfaceBlockKindResponse
{
    Question,
    NextAction,
    ActionGroup,
    Surface
}

/// <summary>One predesigned component of a reply; exactly the member matching <see cref="Kind"/> is set.</summary>
public sealed record GuidedSurfaceBlockResponse(
    GuidedSurfaceBlockKindResponse Kind,
    GuidedSurfaceQuestionResponse? Question = null,
    string? NextAction = null,
    GuidedSurfaceActionGroupResponse? ActionGroup = null,
    GuidedSurfaceSurfaceResponse? Surface = null);

/// <summary>A question the model asked, with its validated prompt and option labels.</summary>
public sealed record GuidedSurfaceQuestionResponse(
    string Prompt,
    IReadOnlyList<GuidedSurfaceOptionResponse> Options,
    int MaxSelections);

public sealed record GuidedSurfaceOptionResponse(string Key, string Label);

public sealed record GuidedSurfaceActionGroupResponse(string AgentRunId, IReadOnlyList<Guid> ActionIds);

/// <summary>
/// A surface in the UI protocol (ADR-0022 decision 8): the component tree the model laid out from the catalog and the
/// data model the server filled from this run's reads, kept separate. <see cref="Protocol"/> and
/// <see cref="ProtocolVersion"/> name the format so a later protocol can convert or decline it.
/// </summary>
public sealed record GuidedSurfaceSurfaceResponse(
    string SurfaceId,
    string Protocol,
    string ProtocolVersion,
    string CatalogId,
    JsonElement Components,
    JsonElement DataModel);
