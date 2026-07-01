namespace Vitarish.Medical.Infrastructure.Providers.Prescriptions.Tamin;

internal sealed record TaminDrugSearchOperationRequest(string? SearchText, string? DrugCode, int Page = 1, int PageSize = 20, bool ActiveOnly = true);
internal sealed record TaminDrugListAllOperationRequest(string? SearchText, string? DrugCode, int PageSize, bool ActiveOnly, int? MaxPages = null);
internal sealed record TaminServiceSearchOperationRequest(string? ServiceType, string? ServiceGroup, string? SearchText, int Page = 1, int PageSize = 20, bool ActiveOnly = true);
internal sealed record TaminServiceListAllOperationRequest(string? ServiceType, string? ServiceGroup, string? SearchText, int PageSize, bool ActiveOnly, int? MaxPages = null);
internal sealed record TaminRawReferenceQueryOperationRequest(string CodeSystem, IReadOnlyDictionary<string, string?> Query);

internal sealed record TaminReferencePageResult(
    string CodeSystem,
    int? Page,
    int? PageSize,
    int? TotalCount,
    bool? HasNextPage,
    string DataVersion,
    IReadOnlyList<TaminReferenceItem> Items,
    string RawJson);

internal sealed record TaminReferenceItem(string Code, string DisplayName, string? Category, bool IsActive, string? ProviderKind, string RawJson);
