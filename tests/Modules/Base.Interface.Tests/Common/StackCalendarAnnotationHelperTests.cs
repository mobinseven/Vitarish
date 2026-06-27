using Vitarish.Modules.Base.Interface.Common;

namespace Modules.Base.Interface.Tests.Common;

public class StackCalendarAnnotationHelperTests
{
    [Fact]
    public void Finds_Annotation_By_Inclusive_Range()
    {
        IReadOnlyList<CalendarAnnotation> annotations =
        [
            new(new DateOnly(2026, 1, 10), new DateOnly(2026, 1, 12), Label: "Range")
        ];

        CalendarAnnotation? result = StackCalendarAnnotationHelper.GetAnnotation(annotations, new DateOnly(2026, 1, 11));

        _ = result.Should().NotBeNull();
        _ = result!.Label.Should().Be("Range");
    }

    [Fact]
    public void Returns_Base_Class_When_Annotation_Class_Missing()
    {
        string css = StackCalendarAnnotationHelper.BuildCssClass("mud-calendar__day", null);

        _ = css.Should().Be("mud-calendar__day");
    }

    [Fact]
    public void Appends_Annotation_Class_When_Present()
    {
        string css = StackCalendarAnnotationHelper.BuildCssClass(
            "mud-calendar__day",
            new CalendarAnnotation(
                new DateOnly(2026, 1, 1),
                new DateOnly(2026, 1, 1),
                HighlightClass: "special"));

        _ = css.Should().Be("mud-calendar__day special");
    }
}
