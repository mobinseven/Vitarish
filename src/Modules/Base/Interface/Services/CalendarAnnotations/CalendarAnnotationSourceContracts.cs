namespace Vitarish.Modules.Base.Interface.Services.CalendarAnnotations;

internal interface ICalendarAnnotationSource
{
    Task<CalendarAnnotationSourceResult> GetAnnotationsAsync(
        CalendarAnnotationSourceRequest request,
        CancellationToken cancellationToken);
}

internal sealed record CalendarAnnotationSourceRequest(
    DateOnly StartDate,
    DateOnly EndDate,
    string CountryCode,
    CultureInfo Culture,
    Uri? ExternalApiEndpoint,
    Uri? IcsFeedUrl);

internal sealed record CalendarAnnotationSourceResult(
    bool IsSuccess,
    IReadOnlyList<CalendarAnnotation> Annotations);
