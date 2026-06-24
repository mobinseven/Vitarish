using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Vitarish.Feed.Application.Queries;
using Vitarish.Feed.Contracts;
using Vitarish.Feed.Domain.Abstractions;
using Vitarish.Feed.Domain.Models;

namespace Vitarish.Feed.Api;

public static class FeedApi
{
    public static IEndpointRouteBuilder MapFeedApi(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder feed = endpoints.MapGroup(FeedRoutes.Base);

        _ = feed.MapGet(FeedRoutes.Items, QueryFeedItemsAsync);
        _ = feed.MapGet(FeedRoutes.Sources, QueryFeedSourcesAsync);
        _ = feed.MapGet(FeedRoutes.Configuration, QueryFeedConfigurationAsync);

        return endpoints;
    }

    private static async Task<IResult> QueryFeedItemsAsync(
        HttpRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        FeedQueryParameters parameters = ReadQueryParameters(request.Query);
        IReadOnlyList<FeedItem> items = await mediator.Send(new GetFeedQuery(ToOptions(parameters)), cancellationToken);
        return Results.Ok(items.Select(ToResponse).ToArray());
    }

    private static async Task<IResult> QueryFeedSourcesAsync(
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<IFeedDataSource> sources = await mediator.Send(new GetFeedSourcesQuery(), cancellationToken);
        return Results.Ok(sources.Select(ToResponse).ToArray());
    }

    private static async Task<IResult> QueryFeedConfigurationAsync(
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        FeedConfiguration configuration = await mediator.Send(new GetFeedConfigurationQuery(), cancellationToken);
        return Results.Ok(ToResponse(configuration));
    }

    private static FeedQueryParameters ReadQueryParameters(IQueryCollection query)
    {
        return new FeedQueryParameters
        {
            Limit = TryReadInt(query, "limit"),
            Skip = TryReadInt(query, "skip"),
            SourceIds = ReadValues(query, "sourceIds"),
            ItemTypes = ReadValues(query, "itemTypes"),
            After = TryReadDateTime(query, "after"),
            Before = TryReadDateTime(query, "before")
        };
    }

    private static FeedQueryOptions ToOptions(FeedQueryParameters parameters) => new()
    {
        Limit = parameters.Limit ?? 50,
        Skip = parameters.Skip ?? 0,
        SourceIds = parameters.SourceIds,
        ItemTypes = parameters.ItemTypes,
        After = parameters.After,
        Before = parameters.Before
    };

    private static FeedItemResponse ToResponse(FeedItem item) => new(
        item.Id,
        item.SourceId,
        item.SourceName,
        item.Title,
        item.Description,
        item.Timestamp,
        item.ItemType,
        item.IconUrl,
        item.NavigationUrl,
        item.CreatedBy,
        item.Metadata);

    private static FeedSourceResponse ToResponse(IFeedDataSource source) => new(
        source.SourceId,
        source.SourceName,
        source.Description,
        source.Priority,
        source.EnabledByDefault);

    private static FeedConfigurationResponse ToResponse(FeedConfiguration configuration) => new(
        configuration.EnabledSourceIds,
        configuration.DefaultPageSize,
        configuration.MaxPageSize,
        configuration.SortOrder,
        configuration.AutoRefresh,
        configuration.AutoRefreshIntervalSeconds,
        configuration.CustomSettings);

    private static int? TryReadInt(IQueryCollection query, string key) =>
        int.TryParse(query[key].FirstOrDefault(), out int value) ? value : null;

    private static DateTime? TryReadDateTime(IQueryCollection query, string key) =>
        DateTime.TryParse(query[key].FirstOrDefault(), out DateTime value) ? value : null;

    private static IReadOnlyList<string>? ReadValues(IQueryCollection query, string key)
    {
        string[] values = query[key]
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)
            .ToArray();

        return values.Length == 0 ? null : values;
    }
}
