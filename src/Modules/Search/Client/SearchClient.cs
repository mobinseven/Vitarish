using System.Net;
using Vitarish.Modules.Base.Client;
using Vitarish.Search.Contracts.Errors;
using Vitarish.Search.Contracts.Requests;
using Vitarish.Search.Contracts.Responses;
using Vitarish.Search.Contracts.Results;
using Vitarish.Search.Contracts.Routes;

namespace Vitarish.Search.Client;

public sealed class SearchClient(HttpClient httpClient) : AuthenticatedModuleApiClient(httpClient), ISearchClient
{
    public async Task<SearchClientResult<SearchResultDto>> SearchAsync(SearchRequest request, CancellationToken cancellationToken = default) =>
        MapResult(await PostJsonAsync<SearchRequest, SearchResultDto>(SearchApiRoutes.Base, request, cancellationToken).ConfigureAwait(false));

    public async Task<SearchClientResult<IReadOnlyList<SearchIndexStatusReportDto>>> GetIndexStatusAsync(GetSearchIndexStatusRequest request, CancellationToken cancellationToken = default) =>
        MapResult(await GetJsonAsync<IReadOnlyList<SearchIndexStatusReportDto>>(SearchApiRoutes.Base + SearchApiRoutes.IndexStatus, cancellationToken).ConfigureAwait(false));

    protected override ModuleApiClientFailure CreateFailure(HttpStatusCode statusCode, string message, int? statusCodeValue) =>
        new(statusCode.ToString(), message, statusCodeValue);

    private static SearchClientResult<T> MapResult<T>(ModuleApiClientResult<T> result) =>
        result.IsSuccess
            ? SearchClientResult<T>.Success(result.Value!)
            : SearchClientResult<T>.Failed(MapFailure(result.Failure!));

    private static SearchError MapFailure(ModuleApiClientFailure failure) =>
        new(MapCode(failure), failure.Message, failure.StatusCode);

    private static SearchErrorCode MapCode(ModuleApiClientFailure failure) => failure.Code switch
    {
        "Canceled" => SearchErrorCode.Canceled,
        "Timeout" => SearchErrorCode.Timeout,
        "Transport" => SearchErrorCode.Transport,
        "Deserialization" => SearchErrorCode.Deserialization,
        nameof(HttpStatusCode.BadRequest) => SearchErrorCode.Validation,
        nameof(HttpStatusCode.Unauthorized) => SearchErrorCode.Unauthorized,
        nameof(HttpStatusCode.Forbidden) => SearchErrorCode.Forbidden,
        _ => SearchErrorCode.Unexpected
    };
}
