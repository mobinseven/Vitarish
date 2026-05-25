namespace Vitarish.Medical.Domain.Entities;

public sealed class MedicalCode : Entity
{
    public Guid CodeSystemReleaseId { get; set; }

    public required string Code { get; set; }

    public required string DisplayName { get; set; }

    public string? LongDescription { get; set; }

    public MedicalCodeStatus Status { get; set; } = MedicalCodeStatus.Draft;

    public DateOnly? EffectiveFrom { get; set; }

    public DateOnly? EffectiveTo { get; set; }

    public RecordOrigin RecordOrigin { get; set; }

    public string? SourceMetadataJson { get; set; }

    public Guid? ImportBatchId { get; set; }
}
