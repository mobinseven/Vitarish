namespace Vitarish.Clinical.Domain.Models;

public sealed record VisitWorkflowReadDto
{
    public Guid Id { get; init; }
    public string PatientFhirReference { get; init; } = string.Empty;
    public string? EncounterFhirReference { get; init; }
    public string? AppointmentFhirReference { get; init; }
    public string VisitType { get; init; } = string.Empty;
    public string? AssignedProviderId { get; init; }
    public string? AssignedLocationId { get; init; }
    public VisitWorkflowStatus Status { get; init; }
    public Guid? WorkflowPolicyId { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
    public string? CreatedBy { get; init; }
}
