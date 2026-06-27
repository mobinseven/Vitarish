using System.Security.Cryptography;
using System.Text;

namespace Vitarish.Modules.Base.Interface.Services.CalendarAnnotations;

internal static class CalendarAnnotationCacheKeyFactory
{
    public static string Create(CalendarAnnotationQuery query, ResolvedCalendarCulture resolvedCulture)
    {
        string payload = string.Join("|",
            query.StartDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            query.EndDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            resolvedCulture.Culture.Name,
            resolvedCulture.CountryCode,
            query.ExternalApiEndpoint?.ToString() ?? "none",
            query.IcsFeedUrl?.ToString() ?? "none");

        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return "calendar-annotations:" + Convert.ToHexString(hash);
    }
}
