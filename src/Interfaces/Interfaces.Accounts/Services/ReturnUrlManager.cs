namespace Vitarish.Interfaces.Accounts.Services;

/// <summary>
/// Manages ReturnUrl using session cookies for reliable persistence across redirects.
/// </summary>
public sealed class ReturnUrlManager(IHttpContextAccessor httpContextAccessor)
{
    private const string ReturnUrlCookieName = "Identity.ReturnUrl";

    private HttpContext HttpContext => httpContextAccessor.HttpContext
        ?? throw new InvalidOperationException("HttpContext is not available.");

    /// <summary>
    /// Gets the stored ReturnUrl from the session cookie, or null if not set.
    /// </summary>
    public string? GetReturnUrl() => HttpContext.Request.Cookies.TryGetValue(ReturnUrlCookieName, out string? value)
            ? string.IsNullOrWhiteSpace(value) ? null : value
            : null;

    /// <summary>
    /// Sets the ReturnUrl in a session cookie.
    /// </summary>
    public void SetReturnUrl(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            ClearReturnUrl();
            return;
        }

        // Validate that the URL is local to prevent open redirect vulnerabilities
        if (!IsLocalUrl(returnUrl))
        {
            throw new ArgumentException("Only local URLs are allowed for ReturnUrl.", nameof(returnUrl));
        }

        if (HttpContext.Response.Headers.IsReadOnly)
        {
            return;
        }

        HttpContext.Response.Cookies.Append(
            ReturnUrlCookieName,
            returnUrl,
            CreateCookieOptions()); // TODO: This line is never reached.

    }

    /// <summary>
    /// Clears the ReturnUrl cookie.
    /// </summary>
    public void ClearReturnUrl() => HttpContext.Response.Cookies.Delete(ReturnUrlCookieName);

    /// <summary>
    /// Gets the ReturnUrl from cookie, clears it, and returns the value.
    /// Used when consuming the ReturnUrl for final redirect.
    /// </summary>
    public string? ConsumeReturnUrl()
    {
        string? value = GetReturnUrl();
        ClearReturnUrl();
        return value;
    }

    /// <summary>
    /// Validates that a URL is local (relative or same-origin) to prevent open redirect vulnerabilities.
    /// </summary>
    private static bool IsLocalUrl(string url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return false;
        }

        // Must be relative (starts with /) or starts with ~/
        if (url[0] == '/')
        {
            // Reject protocol-relative URLs (//)
            return url.Length <= 1 || url[1] != '/';
        }

        if (url.Length > 1 && url[0] == '~' && url[1] == '/')
        {
            return true;
        }

        // Reject absolute URLs
        return false;
    }

    private CookieOptions CreateCookieOptions() => new()
    {
        SameSite = SameSiteMode.Strict,
        HttpOnly = true,
        IsEssential = true,
        Secure = HttpContext.Request.IsHttps, // Only send over HTTPS in production
        // Session cookie - expires when browser closes
        Path = "/",
    };
}
