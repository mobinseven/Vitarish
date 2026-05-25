namespace Vitarish.Medical.Domain.Entities;

public sealed class PackagedDrug : Entity
{
    public Guid DrugProductId { get; set; }

    public required string PackageIdentifier { get; set; }

    public PackageIdentifierSource IdentifierSource { get; set; }

    public string? NdcCode { get; set; }

    public string? PackageSize { get; set; }

    public string? PackageUnit { get; set; }

    public string? PackageDescription { get; set; }

    public DateOnly? MarketingStartDate { get; set; }

    public DateOnly? MarketingEndDate { get; set; }

    public Guid? SourceReleaseId { get; set; }

    public RecordOrigin RecordOrigin { get; set; }

    public Guid? ImportBatchId { get; set; }
}
