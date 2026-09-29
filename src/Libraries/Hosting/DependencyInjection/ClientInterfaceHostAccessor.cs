using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Hosting;

internal sealed class ClientInterfaceHostAccessor(
    IReadOnlyCollection<string> allowedOrigins,
    IHttpContextAccessor httpContextAccessor) : IClientInterfaceHostAccessor
{
    private readonly HashSet<string> allowedOrigins = new(allowedOrigins, StringComparer.OrdinalIgnoreCase);

    public string? ClientInterfaceHost
    {
        get
        {
            StringValues origins = httpContextAccessor.HttpContext?.Request.Headers[HeaderNames.Origin] ?? StringValues.Empty;
            if (origins.Count != 1)
                return null;

            string? origin = origins[0];
            if (string.IsNullOrWhiteSpace(origin) || !Uri.TryCreate(origin, UriKind.Absolute, out Uri? parsedOrigin))
                return null;

            if (parsedOrigin.UserInfo.Length > 0 || parsedOrigin.Query.Length > 0 || parsedOrigin.Fragment.Length > 0 || parsedOrigin.AbsolutePath != "/")
                return null;

            string normalizedOrigin = parsedOrigin.GetLeftPart(UriPartial.Authority);
            return string.Equals(origin, normalizedOrigin, StringComparison.OrdinalIgnoreCase)
                && allowedOrigins.Contains(normalizedOrigin)
                ? parsedOrigin.Host
                : null;
        }
    }
}
