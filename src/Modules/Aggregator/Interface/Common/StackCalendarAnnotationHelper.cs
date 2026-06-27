namespace Vitarish.Modules.Aggregator.Interface.Common;

internal static class StackCalendarAnnotationHelper
{
    public static CalendarAnnotation? GetAnnotation(
        IReadOnlyList<CalendarAnnotation>? annotations,
        DateOnly date) => annotations is null || annotations.Count == 0 ? null : annotations.FirstOrDefault(a => a.Includes(date));

    public static string BuildCssClass(
        string baseCssClass,
        CalendarAnnotation? annotation) => annotation is null || string.IsNullOrWhiteSpace(annotation.HighlightClass)
            ? baseCssClass
            : $"{baseCssClass} {annotation.HighlightClass}";
}
