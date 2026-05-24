namespace Vitarish.Interfaces.Base.Abstractions;

public interface ICalendarAnnotationsService
{
    Task<IReadOnlyList<CalendarAnnotation>> GetAnnotationsAsync(
        CalendarAnnotationQuery query,
        CancellationToken cancellationToken = default);
}
