namespace Vitarish.Sea.Domain.Dto;

/// <summary>
/// Represents a flattened audit row used by the administrative "god view" grids.
/// </summary>
public sealed record GodViewRowReadDto(
    string Primary,
    string? Context,
    string? Details,
    DateTimeOffset CreatedAt,
    string? CreatedBy,
    DateTimeOffset? UpdatedAt,
    string? UpdatedBy,
    DateTimeOffset? DeletedAt,
    string? DeletedBy,
    DateTimeOffset LastActivityAt);

