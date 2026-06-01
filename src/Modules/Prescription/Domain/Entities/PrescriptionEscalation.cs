using Vitarish.Prescription.Domain.Enums;

namespace Vitarish.Prescription.Domain.Entities;

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
    public DateTimeOffset EscalatedAt { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }
}
