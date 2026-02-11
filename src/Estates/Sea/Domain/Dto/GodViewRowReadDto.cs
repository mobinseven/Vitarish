namespace Vitarish.Sea.Domain.Dto;

/// <summary>
/// Represents a flattened audit row used by the administrative "god view" grids.
/// </summary>
public sealed class GodViewRowReadDto
{
    public string Primary { get; set; } = default!;
    public string? Context { get; set; }
    public string? Details { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
    public DateTimeOffset LastActivityAt { get; set; }
}

