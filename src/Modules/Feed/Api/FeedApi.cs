using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Vitarish.Modules.Base.Endpoints;
using Vitarish.Feed.Application.Queries;
using Vitarish.Feed.Contracts;
using Vitarish.Feed.Domain.Abstractions;
using Vitarish.Feed.Domain.Models;

namespace Vitarish.Feed.Api;

public static class FeedApi
{
    public static IEndpointRouteBuilder MapFeedApi(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder feed = endpoints.MapProtectedModuleGroup(FeedRoutes.Base);

        _ = feed.MapGet(FeedRoutes.Items, QueryFeedItemsAsync);
        _ = feed.MapGet(FeedRoutes.Sources, QueryFeedSourcesAsync);
        _ = feed.MapGet(FeedRoutes.Configuration, QueryFeedConfigurationAsync);

        return endpoints;
    }

    private static async Task<IResult> QueryFeedItemsAsync(
        [AsParameters] FeedQueryParameters parameters,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
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

}
