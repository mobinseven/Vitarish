namespace Vitarish.Medical.Domain.Entities;

public sealed class Drug : Entity
{
    public required string PrimaryName { get; set; }

    public required string NormalizedName { get; set; }

    public DrugIdentifierType PrimaryIdentifierType { get; set; }

    public required string PrimaryIdentifierValue { get; set; }

    public DrugStatus Status { get; set; } = DrugStatus.Draft;

    public DateOnly? EffectiveFrom { get; set; }

    public DateOnly? EffectiveTo { get; set; }

    public RecordOrigin RecordOrigin { get; set; }

    public Guid? ImportBatchId { get; set; }
}
