using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Vitarish.Instagram.Scraper;

namespace Vitarish.Instagram.Hosting;

public static class InstagramEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapInstagramEndpoints(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("/api/instagram").WithTags("Instagram");

        group.MapPost("/fetch", HandleFetchAsync)
            .WithName("InstagramScraper.Fetch");

        group.MapPost("/runs", HandleStartRunAsync)
            .WithName("InstagramScraper.StartRun");

        group.MapPost("/run-sync", HandleRunSyncAsync)
            .WithName("InstagramScraper.RunSync");

        return builder;
    }

    public static async Task<Results<Ok<InstagramData[]>, BadRequest<string>>> HandleFetchAsync(
        InstagramDataDescriptor descriptor,
        IInstagramScraperService service,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        try
        {
            InstagramData[] data = await service.FetchAsync(descriptor, cancellationToken).ConfigureAwait(false);
            return TypedResults.Ok(data);
        }
        catch (ArgumentException exception)
        {
            loggerFactory.CreateLogger(typeof(InstagramEndpointRouteBuilderExtensions))
                .LogWarning(exception, "Instagram fetch request rejected: {Message}", exception.Message);
            return TypedResults.BadRequest(exception.Message);
        }
    }

    public static async Task<Results<Ok<InstagramRunInfo>, BadRequest<string>>> HandleStartRunAsync(
        InstagramDataDescriptor descriptor,
        IInstagramScraperService service,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        try
        {
            InstagramRunInfo run = await service.StartRunAsync(descriptor, cancellationToken).ConfigureAwait(false);
            return TypedResults.Ok(run);
        }
        catch (ArgumentException exception)
        {
            loggerFactory.CreateLogger(typeof(InstagramEndpointRouteBuilderExtensions))
                .LogWarning(exception, "Instagram run request rejected: {Message}", exception.Message);
            return TypedResults.BadRequest(exception.Message);
        }
    }

    public static async Task<Results<Ok<InstagramRawOutput>, BadRequest<string>>> HandleRunSyncAsync(
        InstagramDataDescriptor descriptor,
        IInstagramScraperService service,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        try
        {
            InstagramRawOutput output = await service.RunSyncAsync(descriptor, cancellationToken).ConfigureAwait(false);
            return TypedResults.Ok(output);
        }
        catch (ArgumentException exception)
        {
            loggerFactory.CreateLogger(typeof(InstagramEndpointRouteBuilderExtensions))
                .LogWarning(exception, "Instagram run-sync request rejected: {Message}", exception.Message);
            return TypedResults.BadRequest(exception.Message);
        }
    }
}
