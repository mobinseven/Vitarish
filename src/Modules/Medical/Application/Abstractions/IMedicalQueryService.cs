using Vitarish.Medical.Domain.Enums.Prescriptions;

namespace Vitarish.Medical.Application.Abstractions;

public interface IMedicalQueryService
{
    Task<CodeSystem?> GetCodeSystemAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CodeSystem?> GetCodeSystemByKeyAsync(string systemKey, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CodeSystem>> ListCodeSystemsAsync(bool activeOnly = true, CancellationToken cancellationToken = default);

    Task<CodeSystemRelease?> GetCodeSystemReleaseAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CodeSystemRelease?> GetActiveReleaseAsync(string systemKey, DateOnly? asOfDate = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CodeSystemRelease>> ListReleasesAsync(Guid codeSystemId, bool activeOnly = true, CancellationToken cancellationToken = default);

    Task<MedicalCode?> ResolveMedicalCodeAsync(string systemKey, string code, Guid? releaseId = null, DateOnly? asOfDate = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MedicalCode>> SearchMedicalCodesAsync(string systemKey, string textOrCode, Guid? releaseId = null, bool activeOnly = true, DateOnly? asOfDate = null, int limit = 20, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProviderReferenceSearchResult>> SearchPrescriptionReferenceCodesAsync(string providerKey, PrescriptionItemCategory? category, string textOrCode, bool activeOnly = true, DateOnly? asOfDate = null, int limit = 20, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProviderReferenceSearchResult>> SearchProviderReferenceDataAsync(string providerKey, string codeSystem, string text, string? providerCategory = null, int limit = 20, CancellationToken cancellationToken = default);

    Task<ImportBatch?> GetImportBatchAsync(Guid importBatchId, CancellationToken cancellationToken = default);

    Task<Drug?> GetDrugAsync(Guid drugId, DateOnly? asOfDate = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Drug>> SearchDrugsAsync(string text, bool activeOnly = true, DateOnly? asOfDate = null, int limit = 20, CancellationToken cancellationToken = default);
    Task<Drug?> ResolveDrugIdentifierAsync(DrugIdentifierType identifierType, string identifierValue, DateOnly? asOfDate = null, CancellationToken cancellationToken = default);
    Task<DrugProduct?> GetDrugProductAsync(Guid drugProductId, DateOnly? asOfDate = null, CancellationToken cancellationToken = default);
    Task<PackagedDrug?> GetPackagedDrugAsync(Guid packagedDrugId, CancellationToken cancellationToken = default);

    Task<MedicalService?> GetMedicalServiceAsync(Guid serviceId, DateOnly? asOfDate = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MedicalService>> SearchMedicalServicesAsync(string text, string? category = null, string? specialty = null, bool activeOnly = true, DateOnly? asOfDate = null, int limit = 20, CancellationToken cancellationToken = default);
    Task<ServiceCode?> ResolveServiceCodeAsync(string systemKey, string code, Guid? releaseId = null, DateOnly? asOfDate = null, CancellationToken cancellationToken = default);
    Task<ServiceValuation?> GetServiceValuationAsync(Guid serviceCodeId, int year, string? locality = null, FacilityIndicator? facilityIndicator = null, DateOnly? asOfDate = null, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CodeMap>> GetCodeMapsAsync(Guid sourceCodeId, string? targetSystemKey = null, MappingType? mappingType = null, DateOnly? asOfDate = null, CancellationToken cancellationToken = default);
}

public sealed record ProviderReferenceSearchResult(string ProviderKey, string CodeSystem, PrescriptionItemCategory PrescriptionCategory, string ReferenceCode, string DisplayName, string? ProviderCategory, IReadOnlyDictionary<string, string?> Metadata);
