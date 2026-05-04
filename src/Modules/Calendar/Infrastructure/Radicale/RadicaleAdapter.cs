using System.Net;
using System.Net.Http;
using System.Text;
using System.Xml.Linq;
using Microsoft.Extensions.Options;
using Vitarish.Calendar.Infrastructure.Options;

namespace Vitarish.Calendar.Infrastructure.Radicale;

/// <summary>
/// Communicates with a Radicale CalDAV server via HTTP verbs.
/// </summary>
[RegisterTransient]
internal class RadicaleAdapter(
    IHttpClientFactory httpClientFactory,
    IOptions<RadicaleOptions> options)
    : IRadicaleAdapter
{
    private const string HttpClientName = "Radicale";

    private static readonly XNamespace DavNs = "DAV:";
    private static readonly XNamespace CalDavNs = "urn:ietf:params:xml:ns:caldav";

    private HttpClient CreateClient() => httpClientFactory.CreateClient(HttpClientName);

    public async Task CreateCollectionAsync(string principalPath, string slug, string displayName, string timeZone, CancellationToken ct)
    {
        using HttpClient client = CreateClient();
        string collectionUrl = $"{principalPath.TrimEnd('/')}/{slug}/";

        string body = $"""
            <?xml version="1.0" encoding="utf-8" ?>
            <C:mkcalendar xmlns:D="DAV:" xmlns:C="urn:ietf:params:xml:ns:caldav">
              <D:set>
                <D:prop>
                  <D:displayname>{displayName}</D:displayname>
                  <C:calendar-timezone>BEGIN:VCALENDAR
            BEGIN:VTIMEZONE
            TZID:{timeZone}
            END:VTIMEZONE
            END:VCALENDAR</C:calendar-timezone>
                </D:prop>
              </D:set>
            </C:mkcalendar>
            """;

        using HttpRequestMessage request = new(new HttpMethod("MKCALENDAR"), collectionUrl)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/xml")
        };

        HttpResponseMessage response = await client.SendAsync(request, ct);
        await EnsureSuccessAsync(response, collectionUrl);
    }

    public async Task DeleteCollectionAsync(string collectionUrl, CancellationToken ct)
    {
        using HttpClient client = CreateClient();
        HttpResponseMessage response = await client.DeleteAsync(collectionUrl, ct);
        await EnsureSuccessAsync(response, collectionUrl);
    }

    public async Task<string> PutCalendarObjectAsync(string collectionUrl, string objectName, string iCalData, string? ifMatchETag, CancellationToken ct)
    {
        using HttpClient client = CreateClient();
        string resourceUrl = $"{collectionUrl.TrimEnd('/')}/{objectName}.ics";

        using HttpRequestMessage request = new(HttpMethod.Put, resourceUrl)
        {
            Content = new StringContent(iCalData, Encoding.UTF8, "text/calendar")
        };

        if (ifMatchETag is not null)
        {
            request.Headers.TryAddWithoutValidation("If-Match", ifMatchETag);
        }

        HttpResponseMessage response = await client.SendAsync(request, ct);
        await EnsureSuccessAsync(response, resourceUrl);

        return response.Headers.ETag?.Tag ?? string.Empty;
    }

    public async Task<(string iCalData, string eTag)> GetCalendarObjectAsync(string resourceUrl, CancellationToken ct)
    {
        using HttpClient client = CreateClient();
        HttpResponseMessage response = await client.GetAsync(resourceUrl, ct);
        await EnsureSuccessAsync(response, resourceUrl);

        string body = await response.Content.ReadAsStringAsync(ct);
        string eTag = response.Headers.ETag?.Tag ?? string.Empty;
        return (body, eTag);
    }

    public async Task DeleteCalendarObjectAsync(string resourceUrl, string? ifMatchETag, CancellationToken ct)
    {
        using HttpClient client = CreateClient();

        using HttpRequestMessage request = new(HttpMethod.Delete, resourceUrl);
        if (ifMatchETag is not null)
        {
            request.Headers.TryAddWithoutValidation("If-Match", ifMatchETag);
        }

        HttpResponseMessage response = await client.SendAsync(request, ct);
        await EnsureSuccessAsync(response, resourceUrl);
    }

    public async Task<IReadOnlyList<(string url, string iCalData, string eTag)>> QueryTimeRangeAsync(string collectionUrl, TimeRangeQuery range, CancellationToken ct)
    {
        using HttpClient client = CreateClient();

        string start = range.Start.UtcDateTime.ToString("yyyyMMddTHHmmssZ");
        string end = range.End.UtcDateTime.ToString("yyyyMMddTHHmmssZ");

        string body = $"""
            <?xml version="1.0" encoding="utf-8" ?>
            <C:calendar-query xmlns:D="DAV:" xmlns:C="urn:ietf:params:xml:ns:caldav">
              <D:prop>
                <D:getetag/>
                <C:calendar-data/>
              </D:prop>
              <C:filter>
                <C:comp-filter name="VCALENDAR">
                  <C:comp-filter name="VEVENT">
                    <C:time-range start="{start}" end="{end}"/>
                  </C:comp-filter>
                </C:comp-filter>
              </C:filter>
            </C:calendar-query>
            """;

        using HttpRequestMessage request = new(new HttpMethod("REPORT"), collectionUrl)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/xml")
        };
        request.Headers.TryAddWithoutValidation("Depth", "1");

        HttpResponseMessage response = await client.SendAsync(request, ct);
        await EnsureSuccessAsync(response, collectionUrl);

        string xml = await response.Content.ReadAsStringAsync(ct);
        return ParseMultiStatus(xml);
    }

    public async Task<string?> GetETagAsync(string resourceUrl, CancellationToken ct)
    {
        using HttpClient client = CreateClient();

        string body = """
            <?xml version="1.0" encoding="utf-8" ?>
            <D:propfind xmlns:D="DAV:">
              <D:prop><D:getetag/></D:prop>
            </D:propfind>
            """;

        using HttpRequestMessage request = new(new HttpMethod("PROPFIND"), resourceUrl)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/xml")
        };
        request.Headers.TryAddWithoutValidation("Depth", "0");

        HttpResponseMessage response = await client.SendAsync(request, ct);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, resourceUrl);

        string xml = await response.Content.ReadAsStringAsync(ct);
        return ParseFirstETag(xml);
    }

    public async Task<IReadOnlyList<(string url, string eTag)>> ListCollectionObjectsAsync(string collectionUrl, CancellationToken ct)
    {
        using HttpClient client = CreateClient();

        string body = """
            <?xml version="1.0" encoding="utf-8" ?>
            <D:propfind xmlns:D="DAV:">
              <D:prop><D:getetag/></D:prop>
            </D:propfind>
            """;

        using HttpRequestMessage request = new(new HttpMethod("PROPFIND"), collectionUrl)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/xml")
        };
        request.Headers.TryAddWithoutValidation("Depth", "1");

        HttpResponseMessage response = await client.SendAsync(request, ct);
        await EnsureSuccessAsync(response, collectionUrl);

        string xml = await response.Content.ReadAsStringAsync(ct);
        return ParseMultiStatusETagsOnly(xml);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, string resourceUrl)
    {
        switch ((int)response.StatusCode)
        {
            case 200 or 201 or 204 or 207:
                return;
            case 401 or 403:
                throw new RadicaleAuthException(resourceUrl, (int)response.StatusCode);
            case 404:
                throw new RadicaleNotFoundException(resourceUrl);
            case 412:
                throw new RadicaleConflictException(resourceUrl);
            default:
                string body = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Radicale returned HTTP {(int)response.StatusCode} for '{resourceUrl}': {body}");
        }
    }

    private static IReadOnlyList<(string url, string iCalData, string eTag)> ParseMultiStatus(string xml)
    {
        List<(string, string, string)> results = [];

        XDocument doc = XDocument.Parse(xml);
        foreach (XElement response in doc.Descendants(DavNs + "response"))
        {
            string href = response.Element(DavNs + "href")?.Value ?? string.Empty;
            if (string.IsNullOrEmpty(href) || !href.EndsWith(".ics", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string eTag = response.Descendants(DavNs + "getetag").FirstOrDefault()?.Value ?? string.Empty;
            string calData = response.Descendants(CalDavNs + "calendar-data").FirstOrDefault()?.Value ?? string.Empty;

            results.Add((href, calData, eTag));
        }

        return results;
    }

    private static IReadOnlyList<(string url, string eTag)> ParseMultiStatusETagsOnly(string xml)
    {
        List<(string, string)> results = [];

        XDocument doc = XDocument.Parse(xml);
        foreach (XElement response in doc.Descendants(DavNs + "response"))
        {
            string href = response.Element(DavNs + "href")?.Value ?? string.Empty;
            if (string.IsNullOrEmpty(href) || !href.EndsWith(".ics", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string eTag = response.Descendants(DavNs + "getetag").FirstOrDefault()?.Value ?? string.Empty;
            results.Add((href, eTag));
        }

        return results;
    }

    private static string? ParseFirstETag(string xml)
    {
        XDocument doc = XDocument.Parse(xml);
        return doc.Descendants(DavNs + "getetag").FirstOrDefault()?.Value;
    }
}
