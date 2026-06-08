using Vitarish.Medical.Domain.Enums.Prescriptions;

namespace Vitarish.Medical.Domain.Entities.Prescriptions;

/// <summary>
/// Records an escalation of a prescription from the reconciliation queue.
/// </summary>
public sealed class PrescriptionEscalation
{
    public Guid Id { get; set; }
    public Guid PrescriptionId { get; set; }
    public Guid OperatorId { get; set; }
    public required string Summary { get; set; }
    public string? ResolutionNotes { get; set; }
    public PrescriptionStatus StatusAtEscalation { get; set; }
    public DateTime EscalatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
}
