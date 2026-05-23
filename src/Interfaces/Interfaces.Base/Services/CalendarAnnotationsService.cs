using Microsoft.Extensions.Caching.Memory;
using Vitarish.Interfaces.Base.Services.CalendarAnnotations;

namespace Vitarish.Interfaces.Base.Services;

[RegisterScoped]
public sealed class CalendarAnnotationsService : ICalendarAnnotationsService
{
    private static readonly TimeSpan DefaultCacheDuration = TimeSpan.FromHours(6);
    private readonly IReadOnlyList<ICalendarAnnotationSource> sources;
    private readonly IMemoryCache cache;

    public CalendarAnnotationsService(IMemoryCache cache)
        : this(
            cache,
            [
                new ExternalApiCalendarAnnotationSource(),
                new IcsCalendarAnnotationSource(),
                new DeterministicCalendarAnnotationSource()
            ])
    {
    }

    internal CalendarAnnotationsService(
        IMemoryCache cache,
        IReadOnlyList<ICalendarAnnotationSource> sources)
    {
        this.cache = cache;
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

        IReadOnlyList<CalendarAnnotation> annotations = [];
        foreach (ICalendarAnnotationSource source in sources)
        {
            try
            {
                CalendarAnnotationSourceResult result = await source.GetAnnotationsAsync(sourceRequest, cancellationToken);
                if (!result.IsSuccess)
                {
                    continue;
                }

                annotations = result.Annotations;
                break;
            }
            catch
            {
                // Fallback to next source.
            }
        }

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
}
