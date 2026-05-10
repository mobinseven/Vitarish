namespace Vitarish.Clinical.Domain.Models;

public sealed record VisitWorkflowWriteDto
{
    public required string PatientFhirReference { get; init; }
    public required string VisitType { get; init; }
    public string? AppointmentFhirReference { get; init; }
    public string? AssignedProviderId { get; init; }
    public string? AssignedLocationId { get; init; }
    public Guid? WorkflowPolicyId { get; init; }
}
