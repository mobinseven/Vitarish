using Vitarish.Clinical.Domain.Models;

namespace Vitarish.Clinical.Domain.Entities;

public class ClinicalResourceProjection : Entity
{
    public Guid? ResourceLinkId { get; set; }
    public string ResourceType { get; set; } = string.Empty;
    public string ResourceId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string? PatientReference { get; set; }
    public string? ClinicReference { get; set; }
    public string? SourceVersionId { get; set; }
    public DateTimeOffset? LastSyncedAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public FhirOperationState SyncState { get; set; } = FhirOperationState.Pending;
}
