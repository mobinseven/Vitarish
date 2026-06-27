using Microsoft.Extensions.Caching.Memory;
using Vitarish.Modules.Base.Interface.Services;
using Vitarish.Modules.Base.Interface.Services.CalendarAnnotations;

namespace Modules.Base.Interface.Tests.Services;

public class CalendarAnnotationsServiceTests
{
    [Fact]
    public async Task Aggregates_All_Successful_Sources()
    {
        using MemoryCache cache = new(new MemoryCacheOptions());
        FakeSource first = new(_ => new CalendarAnnotationSourceResult(true,
        [
            new CalendarAnnotation(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 1), Label: "A")
        ]));
        FakeSource second = new(_ => new CalendarAnnotationSourceResult(true,
        [
            new CalendarAnnotation(new DateOnly(2026, 1, 2), new DateOnly(2026, 1, 2), Label: "B")
        ]));

        CalendarAnnotationsService service = new(cache, logger: null, [first, second]);

        IReadOnlyList<CalendarAnnotation> result = await service.GetAnnotationsAsync(
            new CalendarAnnotationQuery(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31)));

        _ = result.Should().HaveCount(2);
        _ = result.Select(x => x.Label).Should().BeEquivalentTo(["A", "B"]);
        _ = first.CallCount.Should().Be(1);
        _ = second.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task Falls_Back_When_Source_Throws()
    {
        using MemoryCache cache = new(new MemoryCacheOptions());
        FakeSource first = new(_ => throw new InvalidOperationException("boom"));
        FakeSource second = new(_ => new CalendarAnnotationSourceResult(true,
        [
            new CalendarAnnotation(new DateOnly(2026, 1, 2), new DateOnly(2026, 1, 2), Label: "Fallback")
        ]));

        CalendarAnnotationsService service = new(cache, logger: null, [first, second]);

        IReadOnlyList<CalendarAnnotation> result = await service.GetAnnotationsAsync(
            new CalendarAnnotationQuery(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31)));

        _ = result.Should().HaveCount(1);
        _ = result[0].Label.Should().Be("Fallback");
        _ = first.CallCount.Should().Be(1);
        _ = second.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task Caches_By_Normalized_Query_Key()
    {
        using MemoryCache cache = new(new MemoryCacheOptions());
        FakeSource source = new(_ => new CalendarAnnotationSourceResult(true,
        [
            new CalendarAnnotation(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 1), Label: "Cached")
        ]));

        CalendarAnnotationsService service = new(cache, logger: null, [source]);
        CalendarAnnotationQuery query = new(
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 31),
            Culture: new CultureInfo("en-US"),
            CountryCode: "us",
            ExternalApiEndpoint: new Uri("https://example.test/api/holidays"),
            IcsFeedUrl: new Uri("https://example.test/holidays.ics"));

        IReadOnlyList<CalendarAnnotation> first = await service.GetAnnotationsAsync(query);
        IReadOnlyList<CalendarAnnotation> second = await service.GetAnnotationsAsync(query);

        _ = source.CallCount.Should().Be(1);
        _ = first.Should().HaveCount(1);
        _ = second.Should().HaveCount(1);
    }

    [Fact]
    public async Task Cache_Expires_After_Ttl()
    {
        using MemoryCache cache = new(new MemoryCacheOptions());
        FakeSource source = new(_ => new CalendarAnnotationSourceResult(true,
        [
            new CalendarAnnotation(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 1), Label: "Expiring")
        ]));

        CalendarAnnotationsService service = new(cache, logger: null, [source]);
        CalendarAnnotationQuery query = new(
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 2),
            CacheDuration: TimeSpan.FromMilliseconds(10));

        _ = await service.GetAnnotationsAsync(query);
        await Task.Delay(30);
        _ = await service.GetAnnotationsAsync(query);

        _ = source.CallCount.Should().Be(2);
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
