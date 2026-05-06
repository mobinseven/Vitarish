namespace Vitarish.Clinical.Domain.Models;

public sealed record OperationalProjectionSummary
{
    public Guid WorkflowId { get; init; }
    public string PatientFhirReference { get; init; } = string.Empty;
    public string? EncounterFhirReference { get; init; }
    public string VisitType { get; init; } = string.Empty;
    public VisitWorkflowStatus Status { get; init; }
    public string? AssignedProviderId { get; init; }
    public string? AssignedLocationId { get; init; }
    public string? PatientDisplayName { get; init; }
    public DateTimeOffset WorkflowCreatedAt { get; init; }
    public DateTimeOffset? LastActionAt { get; init; }
    public string ProjectionType { get; init; } = string.Empty;
}
