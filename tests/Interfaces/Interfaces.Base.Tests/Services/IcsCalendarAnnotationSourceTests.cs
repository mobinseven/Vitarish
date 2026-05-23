using Vitarish.Interfaces.Base.Services.CalendarAnnotations;

namespace Interfaces.Base.Tests.Services;

public class IcsCalendarAnnotationSourceTests
{
    [Fact]
    public void Parse_Maps_AllDay_Event_Into_Annotation_Range()
    {
        const string ics = """
BEGIN:VCALENDAR
BEGIN:VEVENT
DTSTART;VALUE=DATE:20260321
DTEND;VALUE=DATE:20260324
SUMMARY:Nowruz
END:VEVENT
END:VCALENDAR
""";

        IReadOnlyList<CalendarAnnotation> result = IcsCalendarAnnotationSource.Parse(
            ics,
            new DateOnly(2026, 3, 1),
            new DateOnly(2026, 3, 31));

        result.Should().ContainSingle();
        CalendarAnnotation annotation = result[0];
        annotation.StartDate.Should().Be(new DateOnly(2026, 3, 21));
        annotation.EndDate.Should().Be(new DateOnly(2026, 3, 23));
        annotation.Label.Should().Be("Nowruz");
    }
}
