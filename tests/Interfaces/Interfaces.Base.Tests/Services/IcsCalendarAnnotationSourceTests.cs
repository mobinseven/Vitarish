using Vitarish.Interfaces.Base.Services.CalendarAnnotations;

namespace Interfaces.Base.Tests.Services;

public class IcsCalendarAnnotationSourceTests
{
    [Fact]
    public async Task ParseAsync_Maps_AllDay_Event_Into_Annotation_Range()
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

        await using MemoryStream stream = new(System.Text.Encoding.UTF8.GetBytes(ics));
        IReadOnlyList<CalendarAnnotation> result = await IcsCalendarAnnotationSource.ParseAsync(
            stream,
            new DateOnly(2026, 3, 1),
            new DateOnly(2026, 3, 31));

        result.Should().ContainSingle();
        CalendarAnnotation annotation = result[0];
        annotation.StartDate.Should().Be(new DateOnly(2026, 3, 21));
        annotation.EndDate.Should().Be(new DateOnly(2026, 3, 23));
        annotation.Label.Should().Be("Nowruz");
    }

    [Fact]
    public async Task ParseAsync_Unfolds_Folded_Lines()
    {
        const string ics = """
BEGIN:VCALENDAR
BEGIN:VEVENT
DTSTART;VALUE=DATE:20260321
DTEND;VALUE=DATE:20260322
SUMMARY:Very long
 folded summary
END:VEVENT
END:VCALENDAR
""";

        await using MemoryStream stream = new(System.Text.Encoding.UTF8.GetBytes(ics));
        IReadOnlyList<CalendarAnnotation> result = await IcsCalendarAnnotationSource.ParseAsync(
            stream,
            new DateOnly(2026, 3, 1),
            new DateOnly(2026, 3, 31));

        result.Should().ContainSingle();
        result[0].Label.Should().Be("Very longfolded summary");
    }
}
