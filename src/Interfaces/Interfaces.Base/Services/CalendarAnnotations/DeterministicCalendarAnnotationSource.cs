namespace Vitarish.Interfaces.Base.Services.CalendarAnnotations;

internal sealed class DeterministicCalendarAnnotationSource : ICalendarAnnotationSource
{
    public Task<CalendarAnnotationSourceResult> GetAnnotationsAsync(
        CalendarAnnotationSourceRequest request,
        CancellationToken cancellationToken)
    {
        List<CalendarAnnotation> annotations = [];

        for (DateOnly date = request.StartDate; date <= request.EndDate; date = date.AddDays(1))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            {
                annotations.Add(new CalendarAnnotation(
                    StartDate: date,
                    EndDate: date,
                    Label: "Weekend",
                    HighlightClass: "mud-calendar__day--annotation-weekend",
                    Source: "Deterministic"));
            }

            if (date.Month == 1 && date.Day == 1)
            {
                annotations.Add(new CalendarAnnotation(
                    StartDate: date,
                    EndDate: date,
                    Label: "New Year",
                    HighlightClass: "mud-calendar__day--annotation-holiday",
                    Source: "Deterministic"));
            }

            if (request.CountryCode == "IR" && date.Month == 3 && date.Day is >= 21 and <= 23)
            {
                annotations.Add(new CalendarAnnotation(
                    StartDate: date,
                    EndDate: date,
                    Label: "Nowruz",
                    HighlightClass: "mud-calendar__day--annotation-holiday",
                    Source: "Deterministic"));
            }
        }

        return Task.FromResult(new CalendarAnnotationSourceResult(true, annotations));
    }
}
