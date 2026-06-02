using Vitarish.Interfaces.Base.Services.CalendarAnnotations;

namespace Interfaces.Base.Tests.Services;

public class CalendarAnnotationCultureResolverTests
{
    [Fact]
    public void Uses_Explicit_CountryCode_When_Provided()
    {
        CalendarAnnotationQuery query = new(
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 2),
            Culture: new CultureInfo("en-US"),
            CountryCode: "ir");

        ResolvedCalendarCulture resolved = CalendarAnnotationCultureResolver.Resolve(query);

        _ = resolved.Culture.Name.Should().Be("en-US");
        _ = resolved.CountryCode.Should().Be("IR");
    }

    [Fact]
    public void Derives_Country_From_Neutral_Culture()
    {
        CalendarAnnotationQuery query = new(
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 2),
            Culture: new CultureInfo("fa"));

        ResolvedCalendarCulture resolved = CalendarAnnotationCultureResolver.Resolve(query);

        _ = resolved.Culture.Name.Should().Be("fa");
        _ = resolved.CountryCode.Should().Be("IR");
    }
}
