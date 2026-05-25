namespace Vitarish.Medical.Domain.Entities;

public sealed class MedicalService : Entity
{
    public required string Name { get; set; }

    public string? Category { get; set; }

    public string? Specialty { get; set; }

    public string? Description { get; set; }

    public MedicalServiceStatus Status { get; set; } = MedicalServiceStatus.Draft;

    public DateOnly? EffectiveFrom { get; set; }

    public DateOnly? EffectiveTo { get; set; }

    public RecordOrigin RecordOrigin { get; set; }

    public Guid? ImportBatchId { get; set; }
}
