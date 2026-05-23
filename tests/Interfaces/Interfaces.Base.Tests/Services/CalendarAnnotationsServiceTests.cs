using Microsoft.Extensions.Caching.Memory;
using Vitarish.Interfaces.Base.Services;
using Vitarish.Interfaces.Base.Services.CalendarAnnotations;

namespace Interfaces.Base.Tests.Services;

public class CalendarAnnotationsServiceTests
{
    [Fact]
    public async Task Uses_First_Successful_Source_And_Skips_Remaining()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var first = new FakeSource(_ => new CalendarAnnotationSourceResult(true,
        [
            new CalendarAnnotation(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 1), Label: "A")
        ]));
        var second = new FakeSource(_ => new CalendarAnnotationSourceResult(true,
        [
            new CalendarAnnotation(new DateOnly(2026, 1, 2), new DateOnly(2026, 1, 2), Label: "B")
        ]));

        var service = new CalendarAnnotationsService(cache, logger: null, [first, second]);

        IReadOnlyList<CalendarAnnotation> result = await service.GetAnnotationsAsync(
            new CalendarAnnotationQuery(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31)));

        result.Should().HaveCount(1);
        result[0].Label.Should().Be("A");
        first.CallCount.Should().Be(1);
        second.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task Falls_Back_When_Source_Throws()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var first = new FakeSource(_ => throw new InvalidOperationException("boom"));
        var second = new FakeSource(_ => new CalendarAnnotationSourceResult(true,
        [
            new CalendarAnnotation(new DateOnly(2026, 1, 2), new DateOnly(2026, 1, 2), Label: "Fallback")
        ]));

        var service = new CalendarAnnotationsService(cache, logger: null, [first, second]);

        IReadOnlyList<CalendarAnnotation> result = await service.GetAnnotationsAsync(
            new CalendarAnnotationQuery(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31)));

        result.Should().HaveCount(1);
        result[0].Label.Should().Be("Fallback");
        first.CallCount.Should().Be(1);
        second.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task Caches_By_Normalized_Query_Key()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var source = new FakeSource(_ => new CalendarAnnotationSourceResult(true,
        [
            new CalendarAnnotation(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 1), Label: "Cached")
        ]));

        var service = new CalendarAnnotationsService(cache, logger: null, [source]);
        var query = new CalendarAnnotationQuery(
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 31),
            Culture: new CultureInfo("en-US"),
            CountryCode: "us",
            ExternalApiEndpoint: new Uri("https://example.test/api/holidays"),
            IcsFeedUrl: new Uri("https://example.test/holidays.ics"));

        IReadOnlyList<CalendarAnnotation> first = await service.GetAnnotationsAsync(query);
        IReadOnlyList<CalendarAnnotation> second = await service.GetAnnotationsAsync(query);

        source.CallCount.Should().Be(1);
        first.Should().HaveCount(1);
        second.Should().HaveCount(1);
    }

    [Fact]
    public async Task Cache_Expires_After_Ttl()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var source = new FakeSource(_ => new CalendarAnnotationSourceResult(true,
        [
            new CalendarAnnotation(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 1), Label: "Expiring")
        ]));

        var service = new CalendarAnnotationsService(cache, logger: null, [source]);
        var query = new CalendarAnnotationQuery(
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 2),
            CacheDuration: TimeSpan.FromMilliseconds(10));

        _ = await service.GetAnnotationsAsync(query);
        await Task.Delay(30);
        _ = await service.GetAnnotationsAsync(query);

        source.CallCount.Should().Be(2);
    }

    private sealed class FakeSource(Func<CalendarAnnotationSourceRequest, CalendarAnnotationSourceResult> handler)
        : ICalendarAnnotationSource
    {
        public int CallCount { get; private set; }

        public Task<CalendarAnnotationSourceResult> GetAnnotationsAsync(
            CalendarAnnotationSourceRequest request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(handler(request));
        }
    }
}
