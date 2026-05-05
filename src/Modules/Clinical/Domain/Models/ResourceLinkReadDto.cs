namespace Vitarish.Clinical.Domain.Models;

public sealed record ResourceLinkReadDto
{
    public Guid Id { get; init; }
    public string LocalEntityType { get; init; } = string.Empty;
    public Guid LocalEntityId { get; init; }
    public string ResourceType { get; init; } = string.Empty;
    public string ResourceId { get; init; } = string.Empty;
    public string? ResourceVersionId { get; init; }
    public string? ETag { get; init; }
    public string? OwnerUserId { get; init; }
    public string? PatientReference { get; init; }
    public bool IsArchived { get; init; }
    public DateTimeOffset? ArchivedAt { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}
