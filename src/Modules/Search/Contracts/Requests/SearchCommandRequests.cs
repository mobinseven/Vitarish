using Vitarish.Search.Contracts.Responses;

namespace Vitarish.Search.Contracts.Requests;

public sealed record RebuildSearchIndexRequest(string IndexName);
public sealed record IndexSearchDocumentRequest(string IndexName, IReadOnlyList<SearchDocumentDto> Documents);
public sealed record DeleteSearchDocumentRequest(string IndexName, string TenantId, string? Type = null, string? CorpusId = null, IReadOnlyDictionary<string, string>? MetadataFilters = null);
