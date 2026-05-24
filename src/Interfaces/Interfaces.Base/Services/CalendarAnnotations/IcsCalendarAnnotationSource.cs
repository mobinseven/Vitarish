using Microsoft.Extensions.Logging;

namespace Vitarish.Interfaces.Base.Services.CalendarAnnotations;

internal sealed class IcsCalendarAnnotationSource : ICalendarAnnotationSource
{
    private readonly IHttpClientFactory httpClientFactory;
    private readonly ILogger<IcsCalendarAnnotationSource>? logger;

    public IcsCalendarAnnotationSource(
        IHttpClientFactory httpClientFactory,
        ILogger<IcsCalendarAnnotationSource>? logger = null)
    {
        this.httpClientFactory = httpClientFactory;
        this.logger = logger;
    }

    public async Task<CalendarAnnotationSourceResult> GetAnnotationsAsync(
        CalendarAnnotationSourceRequest request,
        CancellationToken cancellationToken)
    {
        if (request.IcsFeedUrl is null)
        {
            return new CalendarAnnotationSourceResult(false, []);
        }

        using HttpClient client = CreateClient();
        using HttpResponseMessage response = await client.GetAsync(request.IcsFeedUrl, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger?.LogWarning(
                "Calendar ICS source returned non-success status {StatusCode} for {IcsFeedUrl}.",
                (int)response.StatusCode,
                request.IcsFeedUrl);
            return new CalendarAnnotationSourceResult(false, []);
        }

        await using Stream icsStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        IReadOnlyList<CalendarAnnotation> annotations = await ParseAsync(icsStream, request.StartDate, request.EndDate, cancellationToken);

        return new CalendarAnnotationSourceResult(true, annotations);
    }

    internal static async Task<IReadOnlyList<CalendarAnnotation>> ParseAsync(
        Stream icsStream,
        DateOnly queryStart,
        DateOnly queryEnd,
        CancellationToken cancellationToken = default)
    {
        List<CalendarAnnotation> annotations = [];
        DateOnly? eventStart = null;
        DateOnly? eventEndExclusive = null;
        string? summary = null;
        bool inEvent = false;
        using StreamReader reader = new(icsStream, leaveOpen: true);
        string? pendingLine = null;

        while (await reader.ReadLineAsync(cancellationToken) is { } rawLine)
        {
            if (rawLine.Length == 0)
            {
                continue;
            }

            if (rawLine[0] is ' ' or '\t')
            {
                pendingLine = (pendingLine ?? string.Empty) + rawLine[1..];
                continue;
            }

            if (pendingLine is not null)
            {
                ProcessLine(pendingLine.Trim());
            }

            pendingLine = rawLine;
        }

        if (pendingLine is not null)
        {
            ProcessLine(pendingLine.Trim());
        }

        return annotations;

        void ProcessLine(string line)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (line.Equals("BEGIN:VEVENT", StringComparison.OrdinalIgnoreCase))
            {
                inEvent = true;
                eventStart = null;
                eventEndExclusive = null;
                summary = null;
                return;
            }

            if (!inEvent)
            {
                return;
            }

            if (line.Equals("END:VEVENT", StringComparison.OrdinalIgnoreCase))
            {
                TryAppendCurrentEvent();
                inEvent = false;
                return;
            }

            if (line.StartsWith("DTSTART", StringComparison.OrdinalIgnoreCase))
            {
                eventStart = ParseIcsDateValue(line);
                return;
            }

            if (line.StartsWith("DTEND", StringComparison.OrdinalIgnoreCase))
            {
                eventEndExclusive = ParseIcsDateValue(line);
                return;
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

    internal static IReadOnlyList<CalendarAnnotation> Parse(
        string ics,
        DateOnly queryStart,
        DateOnly queryEnd)
    {
        using MemoryStream stream = new(System.Text.Encoding.UTF8.GetBytes(ics));
        return ParseAsync(stream, queryStart, queryEnd).GetAwaiter().GetResult();
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

    private HttpClient CreateClient() =>
        httpClientFactory.CreateClient("InterfacesBase.CalendarAnnotations");
}
