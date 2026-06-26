using System.Globalization;
using System.Net;
using Vitarish.Modules.Base.Client;
using Vitarish.Requests.Contracts;

namespace Vitarish.Requests.Client;

public sealed class RequestsHttpClient(HttpClient httpClient) : AuthenticatedModuleApiClient(httpClient)
{
    public Task<RequestsClientResult<RequestIdentityPageResponse>> QueryRequestIdentitiesAsync(
        RequestIdentityQueryParameters parameters,
        CancellationToken cancellationToken) =>
        SendQueryAsync<RequestIdentityPageResponse>(BuildIdentitiesUri(parameters), cancellationToken);

    private async Task<RequestsClientResult<T>> SendQueryAsync<T>(string requestUri, CancellationToken cancellationToken) =>
        MapResult(await GetJsonAsync<T>(requestUri, cancellationToken).ConfigureAwait(false));

    private static string BuildIdentitiesUri(RequestIdentityQueryParameters parameters)
    {
        List<string> query = [];
        Add(query, "page", parameters.Page?.ToString(CultureInfo.InvariantCulture));
        Add(query, "pageSize", parameters.PageSize?.ToString(CultureInfo.InvariantCulture));
        Add(query, "search", parameters.Search);
        Add(query, "requestId", parameters.RequestId);
        Add(query, "authority", parameters.Authority);
        Add(query, "clientId", parameters.ClientId);
        Add(query, "ownerId", parameters.OwnerId);
        Add(query, "tenantId", parameters.TenantId);
        Add(query, "userId", parameters.UserId);
        Add(query, "resourceId", parameters.ResourceId);
        Add(query, "traceId", parameters.TraceId);
        Add(query, "additionalInfo", parameters.AdditionalInfo);
        AddSorts(query, parameters.Sorts);

        return query.Count == 0
            ? RequestsRoutes.Base + RequestsRoutes.Identities
            : RequestsRoutes.Base + RequestsRoutes.Identities + "?" + string.Join("&", query);
    }

    private static void Add(List<string> query, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            query.Add($"{Uri.EscapeDataString(key)}={Uri.EscapeDataString(value)}");
        }
    }

    private static void AddSorts(List<string> query, IReadOnlyList<RequestIdentitySortParameter>? sorts)
    {
        if (sorts is null)
        {
            return;
        }

        foreach (RequestIdentitySortParameter sort in sorts.Where(sort => sort is not null && !string.IsNullOrWhiteSpace(sort.Field)))
        {
            Add(query, "sort", sort.Descending ? $"-{sort.Field}" : sort.Field);
        }
    }
    protected override ModuleApiClientFailure CreateFailure(HttpStatusCode statusCode, string message, int? statusCodeValue) =>
        new(statusCode.ToString(), message, statusCodeValue);

    private static RequestsClientResult<T> MapResult<T>(ModuleApiClientResult<T> result) =>
        result.IsSuccess ? RequestsClientResult<T>.Success(result.Value!) : RequestsClientResult<T>.Failed(MapFailure(result.Failure!));

    private static RequestsClientFailure MapFailure(ModuleApiClientFailure failure) =>
        new(MapKind(failure), failure.Message, failure.StatusCode);

    private static RequestsClientFailureKind MapKind(ModuleApiClientFailure failure) => failure.Code switch
    {
        "Canceled" => RequestsClientFailureKind.Canceled,
        "Timeout" => RequestsClientFailureKind.Timeout,
        "Transport" => RequestsClientFailureKind.Transport,
        "Deserialization" => RequestsClientFailureKind.Deserialization,
        nameof(HttpStatusCode.BadRequest) => RequestsClientFailureKind.Validation,
        nameof(HttpStatusCode.Unauthorized) => RequestsClientFailureKind.Unauthorized,
        nameof(HttpStatusCode.Forbidden) => RequestsClientFailureKind.Forbidden,
        nameof(HttpStatusCode.NotFound) => RequestsClientFailureKind.NotFound,
        nameof(HttpStatusCode.Conflict) => RequestsClientFailureKind.Conflict,
        _ => RequestsClientFailureKind.UnexpectedServerFailure
    };

}
