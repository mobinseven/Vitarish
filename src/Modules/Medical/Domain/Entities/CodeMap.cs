namespace Vitarish.Medical.Domain.Entities;

public sealed class CodeMap : Entity
{
    public Guid SourceMedicalCodeId { get; set; }

    public Guid TargetMedicalCodeId { get; set; }

    public MappingType MappingType { get; set; }

    public decimal Confidence { get; set; }

    public CodeMapStatus Status { get; set; } = CodeMapStatus.Draft;

    public DateOnly? EffectiveFrom { get; set; }

    public DateOnly? EffectiveTo { get; set; }

    public RecordOrigin RecordOrigin { get; set; }

    public Guid? ImportBatchId { get; set; }

    public string? SourceNote { get; set; }
}
