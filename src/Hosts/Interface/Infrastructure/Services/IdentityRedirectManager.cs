using System.Diagnostics.CodeAnalysis;

public sealed class IdentityRedirectManager(NavigationManager navigationManager, ILogger<IdentityRedirectManager> logger)
{
    public HttpContext HttpContext { get; set; } = default!;
    public const string StatusCookieName = "Identity.StatusMessage";
    private static readonly CookieBuilder StatusCookieBuilder = new()
    {
        SameSite = SameSiteMode.Strict,
        HttpOnly = true,
        IsEssential = true,
        MaxAge = TimeSpan.FromSeconds(5),
    };

    [DoesNotReturn]
    public void RedirectTo(string? uri)
    {
        uri ??= "";
        if (Uri.IsWellFormedUriString(uri, UriKind.Absolute))
        {
            navigationManager.NavigateTo(uri, true);
        }
        if (!Uri.IsWellFormedUriString(uri, UriKind.Relative))
        {
            uri = navigationManager.ToBaseRelativePath(uri);
        }
        navigationManager.NavigateTo(uri, true);//throws a dumb exception in debugging mode
        //HttpContext.RedirectTo(uri);
        throw new InvalidOperationException($"{nameof(IdentityRedirectManager)} can only be used during static rendering.");
    }

    [DoesNotReturn]
    public void RedirectTo(string uri, Dictionary<string, object?> queryParameters)
    {
        string uriWithoutQuery = navigationManager.ToAbsoluteUri(uri).GetLeftPart(UriPartial.Path);
        string newUri = navigationManager.GetUriWithQueryParameters(uriWithoutQuery, queryParameters);
        RedirectTo(newUri);
    }

    [DoesNotReturn]
    public void RedirectToWithStatus(string uri, string message)
    {
        HttpContext.Response.Cookies.Append(StatusCookieName, message, StatusCookieBuilder.Build(HttpContext));
        RedirectTo(uri);
    }

    private string CurrentPath => navigationManager.ToAbsoluteUri(navigationManager.Uri).GetLeftPart(UriPartial.Query);
}
