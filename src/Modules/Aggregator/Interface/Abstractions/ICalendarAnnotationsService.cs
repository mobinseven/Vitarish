namespace Vitarish.Modules.Aggregator.Interface.Abstractions;

public interface ICalendarAnnotationsService
{
    Task<IReadOnlyList<CalendarAnnotation>> GetAnnotationsAsync(
        CalendarAnnotationQuery query,
        CancellationToken cancellationToken = default);
}
