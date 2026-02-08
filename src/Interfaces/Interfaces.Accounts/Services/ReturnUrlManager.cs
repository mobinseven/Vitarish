using Microsoft.AspNetCore.Http;
using System.Diagnostics.CodeAnalysis;

namespace Vitarish.Interfaces.Accounts.Services;

/// <summary>
/// Manages ReturnUrl and FailureUrl using session cookies for reliable persistence across redirects.
/// </summary>
public sealed class ReturnUrlManager
{
    private const string ReturnUrlCookieName = "Identity.ReturnUrl";
    private const string FailureUrlCookieName = "Identity.FailureUrl";
    
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ReturnUrlManager(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private HttpContext HttpContext => _httpContextAccessor.HttpContext 
        ?? throw new InvalidOperationException("HttpContext is not available.");

    /// <summary>
    /// Gets the stored ReturnUrl from the session cookie, or null if not set.
    /// </summary>
    public string? GetReturnUrl()
    {
        if (HttpContext.Request.Cookies.TryGetValue(ReturnUrlCookieName, out string? value))
        {
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }
        return null;
    }

    /// <summary>
    /// Gets the stored FailureUrl from the session cookie, or null if not set.
    /// </summary>
    public string? GetFailureUrl()
    {
        if (HttpContext.Request.Cookies.TryGetValue(FailureUrlCookieName, out string? value))
        {
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }
        return null;
    }

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

        HttpContext.Response.Cookies.Append(
            ReturnUrlCookieName,
            returnUrl,
            CreateCookieOptions());
    }

    /// <summary>
    /// Sets the FailureUrl in a session cookie.
    /// </summary>
    public void SetFailureUrl(string? failureUrl)
    {
        if (string.IsNullOrWhiteSpace(failureUrl))
        {
            ClearFailureUrl();
            return;
        }

        // Validate that the URL is local to prevent open redirect vulnerabilities
        if (!IsLocalUrl(failureUrl))
        {
            throw new ArgumentException("Only local URLs are allowed for FailureUrl.", nameof(failureUrl));
        }

        HttpContext.Response.Cookies.Append(
            FailureUrlCookieName,
            failureUrl,
            CreateCookieOptions());
    }

    /// <summary>
    /// Clears the ReturnUrl cookie.
    /// </summary>
    public void ClearReturnUrl()
    {
        HttpContext.Response.Cookies.Delete(ReturnUrlCookieName);
    }

    /// <summary>
    /// Clears the FailureUrl cookie.
    /// </summary>
    public void ClearFailureUrl()
    {
        HttpContext.Response.Cookies.Delete(FailureUrlCookieName);
    }

    /// <summary>
    /// Clears both ReturnUrl and FailureUrl cookies.
    /// </summary>
    public void ClearAll()
    {
        ClearReturnUrl();
        ClearFailureUrl();
    }

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
    /// Gets the FailureUrl from cookie, clears it, and returns the value.
    /// Used when consuming the FailureUrl for error redirect.
    /// </summary>
    public string? ConsumeFailureUrl()
    {
        string? value = GetFailureUrl();
        ClearFailureUrl();
        return value;
    }

    /// <summary>
    /// Validates that a URL is local (relative or same-origin) to prevent open redirect vulnerabilities.
    /// </summary>
    private bool IsLocalUrl(string url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return false;
        }

        // Must be relative (starts with /) or starts with ~/
        if (url[0] == '/')
        {
            // Reject protocol-relative URLs (//)
            if (url.Length > 1 && url[1] == '/')
            {
                return false;
            }
            return true;
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
