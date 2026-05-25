namespace Vitarish.Medical.Domain.Entities;

public sealed class DrugProduct : Entity
{
    public Guid DrugId { get; set; }

    public string? BrandName { get; set; }

    public string? GenericName { get; set; }

    public string? DoseForm { get; set; }

    public string? Route { get; set; }

    public string? Strength { get; set; }

    public string? Manufacturer { get; set; }

    public MarketStatus MarketStatus { get; set; } = MarketStatus.Active;

    public DateOnly? EffectiveFrom { get; set; }

    public DateOnly? EffectiveTo { get; set; }

    public RecordOrigin RecordOrigin { get; set; }

    public Guid? ImportBatchId { get; set; }
}
