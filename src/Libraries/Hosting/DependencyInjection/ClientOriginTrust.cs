using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Vitarish.Parties.Application.Abstractions;

namespace Vitarish.Hosting;

/// <summary>The request's single, well-formed <c>Origin</c>, once the host has decided to trust it.</summary>
internal sealed record TrustedClientOrigin(string Origin, string Host);

/// <summary>
/// Browser origins the host trusts: the exact configured origins (<c>Cors:AllowedOrigins</c>),
/// plus an active tenant's own address as answered by Parties. CORS, WebSocket upgrades, and
/// the audited client host all read the per-request <see cref="TrustedClientOrigin"/>.
/// </summary>
internal sealed class ClientOriginTrust(IReadOnlyCollection<string> exactOrigins)
{
    // Normalized as CORS WithOrigins did, so "https://host/" or "https://host:443" still match the browser's origin.
    private readonly HashSet<string> exactOrigins = new(exactOrigins.Select(Normalize), StringComparer.OrdinalIgnoreCase);

    public bool IsExact(string origin) => exactOrigins.Contains(origin);

    /// <summary>
    /// Reads exactly one <c>Origin</c> header that is already in normalized
    /// <c>scheme://host[:port]</c> form, with no user info, path, query, or fragment.
    /// </summary>
    public static bool TryReadSingleOrigin(HttpRequest request, out Uri parsedOrigin, out string normalizedOrigin)
    {
        parsedOrigin = null!;
        normalizedOrigin = string.Empty;
        StringValues origins = request.Headers[HeaderNames.Origin];
        if (origins.Count != 1)
            return false;

        string? origin = origins[0];
        if (string.IsNullOrWhiteSpace(origin) || !Uri.TryCreate(origin, UriKind.Absolute, out Uri? parsed))
            return false;
        if (parsed.UserInfo.Length > 0 || parsed.Query.Length > 0 || parsed.Fragment.Length > 0 || parsed.AbsolutePath != "/")
            return false;

        string normalized = parsed.GetLeftPart(UriPartial.Authority);
        if (!string.Equals(origin, normalized, StringComparison.OrdinalIgnoreCase))
            return false;

        parsedOrigin = parsed;
        normalizedOrigin = normalized;
        return true;
    }

    /// <summary>
    /// Decides once per request whether its origin is trusted and records the answer for
    /// CORS, WebSocket upgrades, and audit. Runs after forwarded headers and before CORS.
    /// When the tenant lookup fails, the origin is not trusted and the request continues, so
    /// same-origin traffic keeps working while cross-origin and WebSocket access fail closed.
    /// </summary>
    public async Task EvaluateAsync(HttpContext context)
    {
        if (!TryReadSingleOrigin(context.Request, out Uri origin, out string normalized))
            return;

        bool trusted = IsExact(normalized);
        if (!trusted && context.RequestServices.GetService<ITenantOriginTrust>() is { } tenantOrigins)
        {
            try
            {
                trusted = await tenantOrigins.IsTenantOriginAsync(origin, context.RequestAborted);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                context.RequestServices.GetService<ILogger<ClientOriginTrust>>()?.LogWarning(
                    exception, "Tenant origin trust is unavailable; origin {OriginHost} is not trusted for this request.", origin.Host);
            }
        }

        if (trusted)
            context.Features.Set(new TrustedClientOrigin(normalized, origin.Host));
    }

    private static string Normalize(string origin) =>
        Uri.TryCreate(origin, UriKind.Absolute, out Uri? parsed) ? parsed.GetLeftPart(UriPartial.Authority) : origin;

    public static bool IsTrustedForRequest(HttpContext? context, string origin) =>
        context?.Features.Get<TrustedClientOrigin>() is { } trusted
        && string.Equals(trusted.Origin, origin, StringComparison.OrdinalIgnoreCase);
}
