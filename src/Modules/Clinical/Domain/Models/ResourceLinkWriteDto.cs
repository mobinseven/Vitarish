namespace Vitarish.Clinical.Domain.Models;

public sealed record ResourceLinkWriteDto
{
    public required string LocalEntityType { get; init; }
    public required Guid LocalEntityId { get; init; }
    public required string ResourceType { get; init; }
    public required string ResourceId { get; init; }
    public string? ResourceVersionId { get; init; }
    public string? OwnerUserId { get; init; }
    public string? PatientReference { get; init; }
}
