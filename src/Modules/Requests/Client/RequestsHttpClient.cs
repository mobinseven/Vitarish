using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Vitarish.Requests.Contracts;

namespace Vitarish.Requests.Client;

public sealed class RequestsHttpClient(HttpClient httpClient)
{
    public Task<RequestsClientResult<RequestIdentityPageResponse>> QueryRequestIdentitiesAsync(
        RequestIdentityQueryParameters parameters,
        CancellationToken cancellationToken) =>
        SendQueryAsync<RequestIdentityPageResponse>(BuildIdentitiesUri(parameters), cancellationToken);

    private async Task<RequestsClientResult<T>> SendQueryAsync<T>(string requestUri, CancellationToken cancellationToken)
    {
        try
        {
            using HttpResponseMessage response = await httpClient.GetAsync(requestUri, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                T? value = await response.Content.ReadFromJsonAsync<T>(cancellationToken);

                return value is null
                    ? RequestsClientResult<T>.Failed(new RequestsClientFailure(RequestsClientFailureKind.Deserialization, "Requests API returned an empty or invalid response.", (int)response.StatusCode))
                    : RequestsClientResult<T>.Success(value);
            }

            return RequestsClientResult<T>.Failed(await CreateFailureAsync(response, cancellationToken));
        }
        catch (OperationCanceledException ex)
        {
            bool isTimeout = ex is TaskCanceledException && !cancellationToken.IsCancellationRequested;
            return RequestsClientResult<T>.Failed(new RequestsClientFailure(
                isTimeout ? RequestsClientFailureKind.Timeout : RequestsClientFailureKind.Canceled,
                isTimeout ? "Requests request timed out." : "Requests request was canceled."));
        }
        catch (TimeoutException)
        {
            return RequestsClientResult<T>.Failed(new RequestsClientFailure(RequestsClientFailureKind.Timeout, "Requests request timed out."));
        }
        catch (HttpRequestException ex)
        {
            return RequestsClientResult<T>.Failed(new RequestsClientFailure(RequestsClientFailureKind.Transport, ex.Message));
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            return RequestsClientResult<T>.Failed(new RequestsClientFailure(RequestsClientFailureKind.Deserialization, ex.Message));
        }
    }

    private static async Task<RequestsClientFailure> CreateFailureAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        RequestsErrorResponse? error = null;
        bool errorPayloadWasUnreadable = false;

        try
        {
            error = response.Content is not null
                ? await response.Content.ReadFromJsonAsync<RequestsErrorResponse>(cancellationToken)
                : null;
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            errorPayloadWasUnreadable = true;
        }

        RequestsClientFailureKind kind = response.StatusCode switch
        {
            HttpStatusCode.BadRequest => RequestsClientFailureKind.Validation,
            HttpStatusCode.Unauthorized => RequestsClientFailureKind.Unauthorized,
            HttpStatusCode.Forbidden => RequestsClientFailureKind.Forbidden,
            HttpStatusCode.NotFound => RequestsClientFailureKind.NotFound,
            HttpStatusCode.Conflict => RequestsClientFailureKind.Conflict,
            _ => RequestsClientFailureKind.UnexpectedServerFailure
        };

        return new RequestsClientFailure(
            kind,
            error?.Message ?? (errorPayloadWasUnreadable ? "Requests API returned an unreadable error response." : "Requests API request failed."),
            (int)response.StatusCode,
            error?.ValidationErrors);
    }

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
        {
            Add(query, "sort", sort.Descending ? $"-{sort.Field}" : sort.Field);
        }
    }
}
