namespace Vitarish.Interfaces.Base.Common;

internal static class StackCalendarAnnotationHelper
{
    public static CalendarAnnotation? GetAnnotation(
        IReadOnlyList<CalendarAnnotation>? annotations,
        DateOnly date)
    {
        if (annotations is null || annotations.Count == 0)
        {
            return null;
        }

        return annotations.FirstOrDefault(a => a.Includes(date));
    }

    public static string BuildCssClass(
        string baseCssClass,
        CalendarAnnotation? annotation)
    {
        if (annotation is null || string.IsNullOrWhiteSpace(annotation.HighlightClass))
        {
            return baseCssClass;
        }

        return $"{baseCssClass} {annotation.HighlightClass}";
    }
}
