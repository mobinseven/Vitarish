using Duende.IdentityServer.Extensions;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Primitives;
using System.Globalization;
using Vitarish.Interfaces.Accounts.Abstractions;

namespace Vitarish.Interfaces.Accounts.Services;

public class UserProfileCultureInfoProvider(
    IUserService userService,
    IHttpContextAccessor httpContextAccessor,
    IJSRuntime jsRuntime) : IUserProfileCultureInfoProvider
{
    private CultureInfo? cachedCulture;
    private bool hasLoaded;
    private Task<IJSObjectReference>? cultureModuleTask;

    public async Task<CultureInfo?> GetRequestedCultureAsync()
    {
        if (hasLoaded)
        {
            return cachedCulture;
        }

        hasLoaded = true;
        cachedCulture = await ResolveCultureAsync();
        return cachedCulture;
    }

    public async Task ApplyRequestedCultureAsync()
    {
        CultureInfo? culture = await GetRequestedCultureAsync();
        if (culture is null)
        {
            return;
        }

        await ApplyCultureAsync(culture);
    }

    public async Task<bool> TryApplyCultureAsync(string? cultureName)
    {
        CultureInfo? culture = TryGetCulture(cultureName);
        if (culture is null)
        {
            return false;
        }

        cachedCulture = culture;
        hasLoaded = true;
        await ApplyCultureAsync(culture);
        return true;
    }

    public async Task SetCultureAsync(string cultureName)
    {
        if (string.IsNullOrWhiteSpace(cultureName))
        {
            return;
        }
        cachedCulture = TryGetCulture(cultureName);
        hasLoaded = true;
        if (cachedCulture is null)
        {
            return;
        }

        await ApplyCultureAsync(cachedCulture);

        string? userId = httpContextAccessor.HttpContext?.GetUserId();
        if (userId is null)
        {
            return;

        }
        await userService.SetCultureAsync(userId, cultureName);
    }

    private async Task<CultureInfo?> ResolveCultureAsync()
    {
        string? cultureFromProfile = await userService.GetCultureAsync(httpContextAccessor.HttpContext?.User.GetSubjectId());
        CultureInfo? culture = TryGetCulture(cultureFromProfile);
        if (culture is not null)
        {
            return culture;
        }

        CultureInfo? cookieCulture = await GetCultureFromCookieAsync();
        if (cookieCulture is null)
        {
            return null;
        }

        var userId = httpContextAccessor.HttpContext?.GetUserId();
        if (userId is not null)
        {
            await userService.SetCultureAsync(userId, cookieCulture.Name);
        }

        return cookieCulture;
    }

    public async Task<CultureInfo?> GetCultureFromCookieAsync()
    {
        HttpContext? context = httpContextAccessor.HttpContext;
        if (context is null)
        {
            return null;
        }

        CookieRequestCultureProvider provider = new();
        ProviderCultureResult? result = await provider.DetermineProviderCultureResult(context);
        if (result is null)
        {
            return null;
        }

        string? cultureName = GetCultureName(result.UICultures)
            ?? GetCultureName(result.Cultures);

        return TryGetCulture(cultureName);
    }

    private static string? GetCultureName(IList<StringSegment>? values)
        => values is null || values.Count == 0 ? null : values[0].Value;

    private async Task ApplyCultureAsync(CultureInfo culture)
    {
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;

        await PersistCultureCookieAsync(culture);
    }

    private async Task PersistCultureCookieAsync(CultureInfo culture)
    {
        HttpContext? context = httpContextAccessor.HttpContext;
        string cookieValue = CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture));
        if (context is not null && !context.Response.HasStarted)
        {
            context.Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                cookieValue,
                new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddYears(1),
                    HttpOnly = false,
                    IsEssential = true,
                    Path = "/",
                    SameSite = SameSiteMode.Lax,
                    Secure = context.Request.IsHttps,
                });
            return;
        }

        IJSObjectReference module = await GetCultureModuleAsync();
        await module.InvokeVoidAsync(
            "setCultureCookie",
            CookieRequestCultureProvider.DefaultCookieName,
            cookieValue);
    }

    private static CultureInfo? TryGetCulture(string? cultureName)
    {
        if (string.IsNullOrWhiteSpace(cultureName))
        {
            return null;
        }

        try
        {
            return CultureInfo.GetCultureInfo(cultureName);
        }
        catch (CultureNotFoundException)
        {
            return null;
        }
    }

    private Task<IJSObjectReference> GetCultureModuleAsync()
        => cultureModuleTask ??= jsRuntime
            .InvokeAsync<IJSObjectReference>(
                "import",
                "/_content/Vitarish.Interfaces.Accounts/Components/Account/CultureSelector.razor.js")
            .AsTask();
}
