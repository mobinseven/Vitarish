namespace Vitarish.Clinical.Domain.Models;

public sealed record ProjectionFilter
{
    public string? ResourceType { get; init; }
    public string? ResourceId { get; init; }
    public Guid? ResourceLinkId { get; init; }
    public FhirOperationState? SyncState { get; init; }
    public string? PatientReference { get; init; }
    public bool IncludeExpired { get; init; }
    public int Take { get; init; } = 50;
}
