namespace Vitarish.Medical.Domain.Entities;

public sealed class ServiceValuation : Entity
{
    public Guid ServiceCodeId { get; set; }

    public int ValuationYear { get; set; }

    public string? Locality { get; set; }

    public FacilityIndicator FacilityIndicator { get; set; }

    public decimal WorkRvu { get; set; }

    public decimal PracticeExpenseRvu { get; set; }

    public decimal MalpracticeRvu { get; set; }

    public decimal? ConversionFactor { get; set; }

    public string? PolicyIndicatorsJson { get; set; }

    public DateOnly? EffectiveFrom { get; set; }

    public DateOnly? EffectiveTo { get; set; }

    public Guid? SourceReleaseId { get; set; }

    public RecordOrigin RecordOrigin { get; set; }

    public Guid? ImportBatchId { get; set; }
}
