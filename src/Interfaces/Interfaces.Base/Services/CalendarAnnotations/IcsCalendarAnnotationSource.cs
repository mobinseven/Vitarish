namespace Vitarish.Interfaces.Base.Services.CalendarAnnotations;

internal sealed class IcsCalendarAnnotationSource : ICalendarAnnotationSource
{
    private static readonly HttpClient HttpClient = new();

    public async Task<CalendarAnnotationSourceResult> GetAnnotationsAsync(
        CalendarAnnotationSourceRequest request,
        CancellationToken cancellationToken)
    {
        if (request.IcsFeedUrl is null)
        {
            return new CalendarAnnotationSourceResult(false, []);
        }

        using HttpResponseMessage response = await HttpClient.GetAsync(request.IcsFeedUrl, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return new CalendarAnnotationSourceResult(false, []);
        }

        string ics = await response.Content.ReadAsStringAsync(cancellationToken);
        IReadOnlyList<CalendarAnnotation> annotations = Parse(ics, request.StartDate, request.EndDate);

        return new CalendarAnnotationSourceResult(true, annotations);
    }

    internal static IReadOnlyList<CalendarAnnotation> Parse(
        string ics,
        DateOnly queryStart,
        DateOnly queryEnd)
    {
        string[] lines = ics
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        List<CalendarAnnotation> annotations = [];
        DateOnly? eventStart = null;
        DateOnly? eventEndExclusive = null;
        string? summary = null;
        bool inEvent = false;

        foreach (string rawLine in lines)
        {
            string line = rawLine.Trim();

            if (line.Equals("BEGIN:VEVENT", StringComparison.OrdinalIgnoreCase))
            {
                inEvent = true;
                eventStart = null;
                eventEndExclusive = null;
                summary = null;
                continue;
            }

            if (!inEvent)
            {
                continue;
            }

            if (line.Equals("END:VEVENT", StringComparison.OrdinalIgnoreCase))
            {
                TryAppendCurrentEvent();
                inEvent = false;
                continue;
            }

            if (line.StartsWith("DTSTART", StringComparison.OrdinalIgnoreCase))
            {
                eventStart = ParseIcsDateValue(line);
                continue;
            }

            if (line.StartsWith("DTEND", StringComparison.OrdinalIgnoreCase))
            {
                eventEndExclusive = ParseIcsDateValue(line);
                continue;
            }

            if (line.StartsWith("SUMMARY", StringComparison.OrdinalIgnoreCase))
            {
                int separator = line.IndexOf(':');
                if (separator >= 0 && separator + 1 < line.Length)
                {
                    summary = line[(separator + 1)..].Trim();
                }
            }
        }

        return annotations;

        void TryAppendCurrentEvent()
        {
            if (!eventStart.HasValue)
            {
                return;
            }

            DateOnly start = eventStart.Value;
            DateOnly inclusiveEnd = eventEndExclusive.HasValue
                ? eventEndExclusive.Value.AddDays(-1)
                : start;

            if (inclusiveEnd < start)
            {
                inclusiveEnd = start;
            }

            if (inclusiveEnd < queryStart || start > queryEnd)
            {
                return;
            }

            DateOnly boundedStart = start < queryStart ? queryStart : start;
            DateOnly boundedEnd = inclusiveEnd > queryEnd ? queryEnd : inclusiveEnd;

            annotations.Add(new CalendarAnnotation(
                StartDate: boundedStart,
                EndDate: boundedEnd,
                Label: summary,
                HighlightClass: "mud-calendar__day--annotation-ics",
                Source: "Ics"));
        }
    }

    private static DateOnly? ParseIcsDateValue(string icsLine)
    {
        int separator = icsLine.IndexOf(':');
        if (separator < 0 || separator + 1 >= icsLine.Length)
        {
            return null;
        }

        string raw = icsLine[(separator + 1)..].Trim();
        if (raw.Length >= 8)
        {
            raw = raw[..8];
        }

        return DateOnly.TryParseExact(raw, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly parsed)
            ? parsed
            : null;
    }
}
