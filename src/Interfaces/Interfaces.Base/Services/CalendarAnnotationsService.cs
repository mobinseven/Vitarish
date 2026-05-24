using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Vitarish.Interfaces.Base.Services.CalendarAnnotations;

namespace Vitarish.Interfaces.Base.Services;

[RegisterScoped]
public sealed class CalendarAnnotationsService : ICalendarAnnotationsService
{
    private static readonly TimeSpan DefaultCacheDuration = TimeSpan.FromHours(6);
    private readonly IReadOnlyList<ICalendarAnnotationSource> sources;
    private readonly IMemoryCache cache;
    private readonly ILogger<CalendarAnnotationsService>? logger;

    public CalendarAnnotationsService(
        IMemoryCache cache,
        IServiceProvider serviceProvider)
        : this(
            cache,
            serviceProvider.GetService<ILogger<CalendarAnnotationsService>>(),
            [
                new ExternalApiCalendarAnnotationSource(
                    serviceProvider.GetRequiredService<IHttpClientFactory>(),
                    serviceProvider.GetService<ILogger<ExternalApiCalendarAnnotationSource>>()),
                new IcsCalendarAnnotationSource(
                    serviceProvider.GetRequiredService<IHttpClientFactory>(),
                    serviceProvider.GetService<ILogger<IcsCalendarAnnotationSource>>()),
                new EmbeddedIcsCalendarAnnotationSource()
            ])
    {
    }

    internal CalendarAnnotationsService(
        IMemoryCache cache,
        ILogger<CalendarAnnotationsService>? logger,
        IReadOnlyList<ICalendarAnnotationSource> sources)
    {
        this.cache = cache;
        this.logger = logger;
        this.sources = sources;
    }

    public async Task<IReadOnlyList<CalendarAnnotation>> GetAnnotationsAsync(
        CalendarAnnotationQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.EndDate < query.StartDate)
        {
            query = query with { StartDate = query.EndDate, EndDate = query.StartDate };
        }

        ResolvedCalendarCulture resolvedCulture = CalendarAnnotationCultureResolver.Resolve(query);
        string cacheKey = CalendarAnnotationCacheKeyFactory.Create(query, resolvedCulture);

        if (cache.TryGetValue(cacheKey, out IReadOnlyList<CalendarAnnotation>? cached) && cached is not null)
        {
            return cached;
        }

        CalendarAnnotationSourceRequest sourceRequest = new(
            StartDate: query.StartDate,
            EndDate: query.EndDate,
            CountryCode: resolvedCulture.CountryCode,
            Culture: resolvedCulture.Culture,
            ExternalApiEndpoint: query.ExternalApiEndpoint,
            IcsFeedUrl: query.IcsFeedUrl);

        Task<CalendarAnnotationSourceResult>[] sourceTasks = sources
            .Select(source => GetSourceResultSafeAsync(source, sourceRequest, cancellationToken))
            .ToArray();

        CalendarAnnotationSourceResult[] sourceResults = await Task.WhenAll(sourceTasks);
        List<CalendarAnnotation> annotations = sourceResults
            .Where(result => result.IsSuccess)
            .SelectMany(result => result.Annotations)
            .ToList();

        TimeSpan ttl = query.CacheDuration.GetValueOrDefault(DefaultCacheDuration);
        if (ttl <= TimeSpan.Zero)
        {
            ttl = DefaultCacheDuration;
        }

        _ = cache.Set(cacheKey, annotations, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ttl
        });

        return annotations;
    }

    private async Task<CalendarAnnotationSourceResult> GetSourceResultSafeAsync(
        ICalendarAnnotationSource source,
        CalendarAnnotationSourceRequest sourceRequest,
        CancellationToken cancellationToken)
    {
        try
        {
            return await source.GetAnnotationsAsync(sourceRequest, cancellationToken);
        }
        catch (Exception ex)
        {
            logger?.LogWarning(
                ex,
                "Calendar annotation source {SourceType} failed and was skipped.",
                source.GetType().Name);
            return new CalendarAnnotationSourceResult(false, []);
        }
    }
}
