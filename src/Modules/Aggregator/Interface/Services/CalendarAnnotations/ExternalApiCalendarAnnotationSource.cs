using Microsoft.Extensions.Logging;
using System.Collections.Specialized;
using System.Web;

namespace Vitarish.Modules.Aggregator.Interface.Services.CalendarAnnotations;

internal sealed class ExternalApiCalendarAnnotationSource(
    IHttpClientFactory httpClientFactory,
    ILogger<ExternalApiCalendarAnnotationSource>? logger = null) : ICalendarAnnotationSource
{
    private readonly IHttpClientFactory httpClientFactory = httpClientFactory;
    private readonly ILogger<ExternalApiCalendarAnnotationSource>? logger = logger;

    public async Task<CalendarAnnotationSourceResult> GetAnnotationsAsync(
        CalendarAnnotationSourceRequest request,
        CancellationToken cancellationToken)
    {
        if (request.ExternalApiEndpoint is null)
        {
            return new CalendarAnnotationSourceResult(false, []);
        }

        Uri endpoint = BuildEndpoint(request.ExternalApiEndpoint, request);

        using HttpClient client = CreateClient();
        using HttpResponseMessage response = await client.GetAsync(endpoint, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger?.LogWarning(
                "Calendar external API source returned non-success status {StatusCode} for {Endpoint}.",
                (int)response.StatusCode,
                endpoint);
            return new CalendarAnnotationSourceResult(false, []);
        }

        await using Stream payload = await response.Content.ReadAsStreamAsync(cancellationToken);
        using JsonDocument document = await JsonDocument.ParseAsync(payload, cancellationToken: cancellationToken);

        List<CalendarAnnotation> annotations = Parse(document);
        return new CalendarAnnotationSourceResult(true, annotations);
    }

    private HttpClient CreateClient() =>
        httpClientFactory.CreateClient("ModulesAggregatorInterface.CalendarAnnotations");

    private static Uri BuildEndpoint(Uri endpoint, CalendarAnnotationSourceRequest request)
    {
        NameValueCollection query = HttpUtility.ParseQueryString(endpoint.Query);
        query["startDate"] = request.StartDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        query["endDate"] = request.EndDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        query["countryCode"] = request.CountryCode;
        query["culture"] = request.Culture.Name;

        UriBuilder builder = new(endpoint)
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
        return !string.IsNullOrWhiteSpace(value) && DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
    }
}
