namespace Vitarish.Modules.Base.Interface.Abstractions;

public interface ICalendarAnnotationsService
{
    Task<IReadOnlyList<CalendarAnnotation>> GetAnnotationsAsync(
        CalendarAnnotationQuery query,
        CancellationToken cancellationToken = default);
}
