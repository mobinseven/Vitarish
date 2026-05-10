using Vitarish.Clinical.Domain.Models;

namespace Vitarish.Clinical.Domain.Entities;

public sealed class ClinicalActionRecord : Entity
{
    public Guid WorkflowId { get; set; }
    public ClinicalActionType ActionType { get; set; }
    public string? ActorId { get; set; }
    public string? RequestJson { get; set; }
    public string ValidationStatus { get; set; } = "Pending";
    public string CommitStatus { get; set; } = "Pending";
    public string? FhirTransactionReference { get; set; }
    public string? ErrorDetailsJson { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
}
