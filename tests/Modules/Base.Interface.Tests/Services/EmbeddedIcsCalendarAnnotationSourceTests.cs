using Vitarish.Modules.Base.Interface.Services.CalendarAnnotations;

namespace Modules.Base.Interface.Tests.Services;

public class EmbeddedIcsCalendarAnnotationSourceTests
{
    [Fact]
    public void ReadEmbeddedIcs_Returns_Content_For_Known_Country()
    {
        string? content = EmbeddedIcsCalendarAnnotationSource.ReadEmbeddedIcs("IR");

        _ = content.Should().NotBeNullOrWhiteSpace();
        _ = content.Should().Contain("BEGIN:VCALENDAR");
        _ = content.Should().Contain("BEGIN:VEVENT");
    }

    [Fact]
    public void ReadEmbeddedIcs_Returns_Null_For_Unknown_Country()
    {
        string? content = EmbeddedIcsCalendarAnnotationSource.ReadEmbeddedIcs("ZZ");

        _ = content.Should().BeNull();
    }

    [Fact]
    public void ReadEmbeddedIcs_Normalizes_CountryCode_CaseInsensitively()
    {
        string? lower = EmbeddedIcsCalendarAnnotationSource.ReadEmbeddedIcs("ir");
        string? upper = EmbeddedIcsCalendarAnnotationSource.ReadEmbeddedIcs("IR");
        string? mixed = EmbeddedIcsCalendarAnnotationSource.ReadEmbeddedIcs("Ir");

        _ = lower.Should().NotBeNull();
        _ = lower.Should().Be(upper);
        _ = lower.Should().Be(mixed);
    }

    [Fact]
    public async Task GetAnnotationsAsync_Returns_Annotations_For_IR_With_Nowruz_In_Range()
    {
        EmbeddedIcsCalendarAnnotationSource source = new();
        CalendarAnnotationSourceRequest request = new(
            StartDate: new DateOnly(2026, 3, 1),
            EndDate: new DateOnly(2026, 3, 31),
            CountryCode: "IR",
            Culture: new CultureInfo("fa"),
            ExternalApiEndpoint: null,
            IcsFeedUrl: null);

        CalendarAnnotationSourceResult result = await source.GetAnnotationsAsync(request, CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = result.Annotations.Should().Contain(a => a.Label == "Nowruz");
    }

    [Fact]
    public async Task GetAnnotationsAsync_Returns_Failure_When_No_Resource_For_CountryCode()
    {
        EmbeddedIcsCalendarAnnotationSource source = new();
        CalendarAnnotationSourceRequest request = new(
            StartDate: new DateOnly(2026, 1, 1),
            EndDate: new DateOnly(2026, 1, 31),
            CountryCode: "ZZ",
            Culture: new CultureInfo("en"),
            ExternalApiEndpoint: null,
            IcsFeedUrl: null);

        CalendarAnnotationSourceResult result = await source.GetAnnotationsAsync(request, CancellationToken.None);

        _ = result.IsSuccess.Should().BeFalse();
        _ = result.Annotations.Should().BeEmpty();
    }
}
