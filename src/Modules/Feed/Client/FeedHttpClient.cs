using System.Globalization;
using System.Net;
using Vitarish.Modules.Base.Client;
using Vitarish.Feed.Contracts;

namespace Vitarish.Feed.Client;

public sealed class FeedHttpClient(HttpClient httpClient) : AuthenticatedModuleApiClient(httpClient), IFeedClient
{
    public Task<FeedClientResult<IReadOnlyList<FeedItemResponse>>> QueryFeedItemsAsync(
        FeedQueryParameters parameters,
        IReadOnlyList<string>? sourceIds,
        IReadOnlyList<string>? itemTypes,
        CancellationToken cancellationToken) =>
        SendQueryAsync<IReadOnlyList<FeedItemResponse>>(BuildItemsUri(parameters, sourceIds, itemTypes), cancellationToken);

    public Task<FeedClientResult<IReadOnlyList<FeedSourceResponse>>> QueryFeedSourcesAsync(
        CancellationToken cancellationToken) =>
        SendQueryAsync<IReadOnlyList<FeedSourceResponse>>(FeedRoutes.Base + FeedRoutes.Sources, cancellationToken);

    public Task<FeedClientResult<FeedConfigurationResponse>> QueryFeedConfigurationAsync(
        CancellationToken cancellationToken) =>
        SendQueryAsync<FeedConfigurationResponse>(FeedRoutes.Base + FeedRoutes.Configuration, cancellationToken);

    private async Task<FeedClientResult<T>> SendQueryAsync<T>(string requestUri, CancellationToken cancellationToken) =>
        MapResult(await GetJsonAsync<T>(requestUri, cancellationToken).ConfigureAwait(false));

    private static string BuildItemsUri(FeedQueryParameters parameters, IReadOnlyList<string>? sourceIds, IReadOnlyList<string>? itemTypes)
    {
        List<string> query = [];
        Add(query, "limit", parameters.Limit?.ToString(CultureInfo.InvariantCulture));
        Add(query, "skip", parameters.Skip?.ToString(CultureInfo.InvariantCulture));
        Add(query, "after", parameters.After?.ToString("O", CultureInfo.InvariantCulture));
        Add(query, "before", parameters.Before?.ToString("O", CultureInfo.InvariantCulture));
        AddMany(query, "sourceIds", sourceIds);
        AddMany(query, "itemTypes", itemTypes);

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
    protected override ModuleApiClientFailure CreateFailure(HttpStatusCode statusCode, string message, int? statusCodeValue) =>
        new(statusCode.ToString(), message, statusCodeValue);

    private static FeedClientResult<T> MapResult<T>(ModuleApiClientResult<T> result) =>
        result.IsSuccess ? FeedClientResult<T>.Success(result.Value!) : FeedClientResult<T>.Failed(MapFailure(result.Failure!));

    private static FeedClientFailure MapFailure(ModuleApiClientFailure failure) =>
        new(MapKind(failure), failure.Message, failure.StatusCode);

    private static FeedClientFailureKind MapKind(ModuleApiClientFailure failure) => failure.Code switch
    {
        "Canceled" => FeedClientFailureKind.Canceled,
        "Timeout" => FeedClientFailureKind.Timeout,
        "Transport" => FeedClientFailureKind.Transport,
        "Deserialization" => FeedClientFailureKind.Deserialization,
        nameof(HttpStatusCode.BadRequest) => FeedClientFailureKind.Validation,
        nameof(HttpStatusCode.Unauthorized) => FeedClientFailureKind.Unauthorized,
        nameof(HttpStatusCode.Forbidden) => FeedClientFailureKind.Forbidden,
        nameof(HttpStatusCode.NotFound) => FeedClientFailureKind.NotFound,
        nameof(HttpStatusCode.Conflict) => FeedClientFailureKind.Conflict,
        _ => FeedClientFailureKind.UnexpectedServerFailure
    };

}
