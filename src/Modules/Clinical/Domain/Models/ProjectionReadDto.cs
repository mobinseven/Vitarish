namespace Vitarish.Clinical.Domain.Models;

public sealed record ProjectionReadDto
{
    public Guid Id { get; init; }
    public Guid? ResourceLinkId { get; init; }
    public string ResourceType { get; init; } = string.Empty;
    public string ResourceId { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string? Summary { get; init; }
    public string? PatientReference { get; init; }
    public string? ClinicReference { get; init; }
    public string? SourceVersionId { get; init; }
    public DateTimeOffset? LastSyncedAt { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
    public FhirOperationState SyncState { get; init; }
}
