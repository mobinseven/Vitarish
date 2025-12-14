namespace Vitarish.Clients.Modules.Sea.Dto;

/// <summary>
/// Represents a flattened audit row used by the administrative "god view" grids.
/// </summary>
public sealed record GodViewRowDto(
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

