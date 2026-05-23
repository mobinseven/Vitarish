using System.Text.Json;
using System.Web;

namespace Vitarish.Interfaces.Base.Services.CalendarAnnotations;

internal sealed class ExternalApiCalendarAnnotationSource : ICalendarAnnotationSource
{
    private static readonly HttpClient HttpClient = new();

    public async Task<CalendarAnnotationSourceResult> GetAnnotationsAsync(
        CalendarAnnotationSourceRequest request,
        CancellationToken cancellationToken)
    {
        if (request.ExternalApiEndpoint is null)
        {
            return new CalendarAnnotationSourceResult(false, []);
        }

        Uri endpoint = BuildEndpoint(request.ExternalApiEndpoint, request);
        using HttpResponseMessage response = await HttpClient.GetAsync(endpoint, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return new CalendarAnnotationSourceResult(false, []);
        }

        await using Stream payload = await response.Content.ReadAsStreamAsync(cancellationToken);
        using JsonDocument document = await JsonDocument.ParseAsync(payload, cancellationToken: cancellationToken);

        List<CalendarAnnotation> annotations = Parse(document);
        return new CalendarAnnotationSourceResult(true, annotations);
    }

    private static Uri BuildEndpoint(Uri endpoint, CalendarAnnotationSourceRequest request)
    {
        var query = HttpUtility.ParseQueryString(endpoint.Query);
        query["startDate"] = request.StartDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        query["endDate"] = request.EndDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        query["countryCode"] = request.CountryCode;
        query["culture"] = request.Culture.Name;

        var builder = new UriBuilder(endpoint)
        {
            Query = query.ToString() ?? string.Empty
        };

        return builder.Uri;
    }

    private static List<CalendarAnnotation> Parse(JsonDocument document)
    {
        JsonElement root = document.RootElement;
        JsonElement listElement = root.ValueKind == JsonValueKind.Array
            ? root
            : root.TryGetProperty("annotations", out JsonElement nested) && nested.ValueKind == JsonValueKind.Array
                ? nested
                : default;

        if (listElement.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        List<CalendarAnnotation> annotations = [];
        foreach (JsonElement item in listElement.EnumerateArray())
        {
            if (!TryParseDateRange(item, out DateOnly start, out DateOnly end))
            {
                continue;
            }

            string? label = item.TryGetProperty("label", out JsonElement labelValue) ? labelValue.GetString() : null;
            string? highlightClass = item.TryGetProperty("highlightClass", out JsonElement classValue) ? classValue.GetString() : null;
            string? highlightStyle = item.TryGetProperty("highlightStyle", out JsonElement styleValue) ? styleValue.GetString() : null;

            annotations.Add(new CalendarAnnotation(
                StartDate: start,
                EndDate: end,
                Label: label,
                HighlightClass: highlightClass,
                HighlightStyle: highlightStyle,
                Source: "ExternalApi"));
        }

        return annotations;
    }

    private static bool TryParseDateRange(JsonElement item, out DateOnly start, out DateOnly end)
    {
        start = default;
        end = default;

        if (!TryParseDate(item, "startDate", out start) &&
            !TryParseDate(item, "date", out start))
        {
            return false;
        }

        if (!TryParseDate(item, "endDate", out end))
        {
            end = start;
        }

        if (end < start)
        {
            (start, end) = (end, start);
        }

        return true;
    }

    private static bool TryParseDate(JsonElement item, string propertyName, out DateOnly date)
    {
        date = default;
        if (!item.TryGetProperty(propertyName, out JsonElement dateElement))
        {
            return false;
        }

        string? value = dateElement.GetString();
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
    }
}
