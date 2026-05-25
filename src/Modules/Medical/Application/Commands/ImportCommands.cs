using Vitarish.Entities.Server.Models;

namespace Vitarish.Medical.Application.Commands;

public record ImportCodeSystemReleaseCommand(
    Guid CodeSystemId,
    string ReleaseName,
    DateOnly? ReleaseDate,
    DateOnly? EffectiveFrom,
    DateOnly? EffectiveTo,
    string SourceLocator,
    string SourceVersion,
    string? SourceChecksum,
    ImportSourceType SourceType,
    IReadOnlyList<ImportMedicalCodeItem> Codes,
    bool DryRun = false) : IRequest<EntityResult<ImportResult>>;

public record ImportMedicalCodeItem(
    string Code,
    string DisplayName,
    string? LongDescription,
    DateOnly? EffectiveFrom,
    DateOnly? EffectiveTo,
    string? SourceMetadataJson);

public record ImportDrugCatalogCommand(
    string SourceLocator,
    string SourceVersion,
    string? SourceChecksum,
    ImportSourceType SourceType,
    IReadOnlyList<ImportDrugItem> Drugs,
    bool DryRun = false) : IRequest<EntityResult<ImportResult>>;

public record ImportDrugItem(
    string PrimaryName,
    DrugIdentifierType PrimaryIdentifierType,
    string PrimaryIdentifierValue,
    DateOnly? EffectiveFrom,
    DateOnly? EffectiveTo,
    IReadOnlyList<ImportDrugProductItem>? Products);

public record ImportDrugProductItem(
    string? BrandName,
    string? GenericName,
    string? DoseForm,
    string? Route,
    string? Strength,
    string? Manufacturer,
    MarketStatus MarketStatus,
    IReadOnlyList<ImportPackagedDrugItem>? Packages);

public record ImportPackagedDrugItem(
    string PackageIdentifier,
    PackageIdentifierSource IdentifierSource,
    string? NdcCode,
    string? PackageSize,
    string? PackageUnit,
    string? PackageDescription,
    DateOnly? MarketingStartDate,
    DateOnly? MarketingEndDate);

public record ImportServiceCatalogCommand(
    string SourceLocator,
    string SourceVersion,
    string? SourceChecksum,
    ImportSourceType SourceType,
    Guid CodeSystemId,
    string ReleaseName,
    IReadOnlyList<ImportServiceItem> Services,
    bool DryRun = false) : IRequest<EntityResult<ImportResult>>;

public record ImportServiceItem(
    string Name,
    string? Category,
    string? Specialty,
    string? Description,
    IReadOnlyList<ImportServiceCodeItem>? Codes);

public record ImportServiceCodeItem(
    string Code,
    string? DisplayName,
    bool IsPrimary);

public record ImportServiceValuationsCommand(
    string SourceLocator,
    string SourceVersion,
    string? SourceChecksum,
    ImportSourceType SourceType,
    Guid? SourceReleaseId,
    IReadOnlyList<ImportServiceValuationItem> Valuations,
    bool DryRun = false) : IRequest<EntityResult<ImportResult>>;

public record ImportServiceValuationItem(
    Guid ServiceCodeId,
    int ValuationYear,
    string? Locality,
    FacilityIndicator FacilityIndicator,
    decimal WorkRvu,
    decimal PracticeExpenseRvu,
    decimal MalpracticeRvu,
    decimal? ConversionFactor,
    string? PolicyIndicatorsJson,
    DateOnly? EffectiveFrom,
    DateOnly? EffectiveTo);
