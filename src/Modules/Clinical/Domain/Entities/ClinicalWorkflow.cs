using Vitarish.Clinical.Domain.Models;

namespace Vitarish.Clinical.Domain.Entities;

public sealed class ClinicalWorkflow : Entity
{
    public string PatientFhirReference { get; set; } = string.Empty;
    public string? EncounterFhirReference { get; set; }
    public string? AppointmentFhirReference { get; set; }
    public string VisitType { get; set; } = string.Empty;
    public string? AssignedProviderId { get; set; }
    public string? AssignedLocationId { get; set; }
    public VisitWorkflowStatus Status { get; set; } = VisitWorkflowStatus.Draft;
    public Guid? WorkflowPolicyId { get; set; }
}
