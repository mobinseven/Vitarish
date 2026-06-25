using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Vitarish.Modules.Base.Client;
using Vitarish.Feed.Contracts;

namespace Vitarish.Feed.Client;

public sealed class FeedHttpClient(HttpClient httpClient) : AuthenticatedModuleApiClient(httpClient)
{
    public Task<FeedClientResult<IReadOnlyList<FeedItemResponse>>> QueryFeedItemsAsync(
        FeedQueryParameters parameters,
        CancellationToken cancellationToken) =>
        SendQueryAsync<IReadOnlyList<FeedItemResponse>>(BuildItemsUri(parameters), cancellationToken);

    public Task<FeedClientResult<IReadOnlyList<FeedSourceResponse>>> QueryFeedSourcesAsync(
        CancellationToken cancellationToken) =>
        SendQueryAsync<IReadOnlyList<FeedSourceResponse>>(FeedRoutes.Base + FeedRoutes.Sources, cancellationToken);

    public Task<FeedClientResult<FeedConfigurationResponse>> QueryFeedConfigurationAsync(
        CancellationToken cancellationToken) =>
        SendQueryAsync<FeedConfigurationResponse>(FeedRoutes.Base + FeedRoutes.Configuration, cancellationToken);

    private async Task<FeedClientResult<T>> SendQueryAsync<T>(string requestUri, CancellationToken cancellationToken)
    {
        try
        {
            using HttpResponseMessage response = await httpClient.GetAsync(requestUri, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                T? value = await response.Content.ReadFromJsonAsync<T>(cancellationToken);

                return value is null
                    ? FeedClientResult<T>.Failed(new FeedClientFailure(FeedClientFailureKind.Deserialization, "Feed API returned an empty or invalid response.", (int)response.StatusCode))
                    : FeedClientResult<T>.Success(value);
            }

            return FeedClientResult<T>.Failed(await CreateFailureAsync(response, cancellationToken));
        }
        catch (OperationCanceledException ex)
        {
            bool isTimeout = ex is TaskCanceledException && !cancellationToken.IsCancellationRequested;
            return FeedClientResult<T>.Failed(new FeedClientFailure(
                isTimeout ? FeedClientFailureKind.Timeout : FeedClientFailureKind.Canceled,
                isTimeout ? "Feed request timed out." : "Feed request was canceled."));
        }
        catch (TimeoutException)
        {
            return FeedClientResult<T>.Failed(new FeedClientFailure(FeedClientFailureKind.Timeout, "Feed request timed out."));
        }
        catch (HttpRequestException ex)
        {
            return FeedClientResult<T>.Failed(new FeedClientFailure(FeedClientFailureKind.Transport, ex.Message));
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            return FeedClientResult<T>.Failed(new FeedClientFailure(FeedClientFailureKind.Deserialization, ex.Message));
        }
    }

    private static async Task<FeedClientFailure> CreateFailureAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        FeedErrorResponse? error = null;
        bool errorPayloadWasUnreadable = false;

        try
        {
            error = await response.Content.ReadFromJsonAsync<FeedErrorResponse>(cancellationToken);
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            errorPayloadWasUnreadable = true;
        }

        FeedClientFailureKind kind = response.StatusCode switch
        {
            HttpStatusCode.BadRequest => FeedClientFailureKind.Validation,
            HttpStatusCode.Unauthorized => FeedClientFailureKind.Unauthorized,
            HttpStatusCode.Forbidden => FeedClientFailureKind.Forbidden,
            HttpStatusCode.NotFound => FeedClientFailureKind.NotFound,
            HttpStatusCode.Conflict => FeedClientFailureKind.Conflict,
            _ => FeedClientFailureKind.UnexpectedServerFailure
        };

        return new FeedClientFailure(
            kind,
            error?.Message ?? (errorPayloadWasUnreadable ? "Feed API returned an unreadable error response." : "Feed API request failed."),
            (int)response.StatusCode,
            error?.ValidationErrors);
    }

    private static string BuildItemsUri(FeedQueryParameters parameters)
    {
        List<string> query = [];
        Add(query, "limit", parameters.Limit?.ToString(CultureInfo.InvariantCulture));
        Add(query, "skip", parameters.Skip?.ToString(CultureInfo.InvariantCulture));
        Add(query, "after", parameters.After?.ToString("O", CultureInfo.InvariantCulture));
        Add(query, "before", parameters.Before?.ToString("O", CultureInfo.InvariantCulture));
        AddMany(query, "sourceIds", parameters.SourceIds);
        AddMany(query, "itemTypes", parameters.ItemTypes);

        return query.Count == 0
            ? FeedRoutes.Base + FeedRoutes.Items
            : FeedRoutes.Base + FeedRoutes.Items + "?" + string.Join("&", query);
    }

    private static void Add(List<string> query, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            query.Add($"{Uri.EscapeDataString(key)}={Uri.EscapeDataString(value)}");
        }
    }

    private static void AddMany(List<string> query, string key, IReadOnlyList<string>? values)
    {
        if (values is null)
        {
            return;
        }

        foreach (string value in values.Where(value => !string.IsNullOrWhiteSpace(value)))
        {
            Add(query, key, value);
        }
    }
    protected override ModuleApiClientFailure CreateFailure(HttpStatusCode statusCode, string message, int? statusCodeValue) => new(statusCode.ToString(), message, statusCodeValue);

}
