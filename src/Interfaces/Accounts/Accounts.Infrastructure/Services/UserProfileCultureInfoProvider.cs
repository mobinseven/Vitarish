using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Primitives;
using Microsoft.JSInterop;
using Vitarish.Accounts.Application.Abstractions;

namespace Vitarish.Accounts.Infrastructure.Services;

public class UserProfileCultureInfoProvider(
    UserProfileInterfaceService profileService,
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

    public async Task<bool> SetCultureAsync(string cultureName)
    {
        if (string.IsNullOrWhiteSpace(cultureName))
        {
            return false;
        }

        if (!await profileService.SetCultureAsync(cultureName))
        {
            return false;
        }

        cachedCulture = TryGetCulture(cultureName);
        hasLoaded = true;
        if (cachedCulture is null)
        {
            return false;
        }

        await ApplyCultureAsync(cachedCulture);
        return true;
    }

    private async Task<CultureInfo?> ResolveCultureAsync()
    {
        Dictionary<string, string>? preferences = await profileService.GetProfileAsync();
        string? cultureFromProfile = preferences is null ? null : profileService.GetCulture(preferences);
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

        if (preferences is not null)
        {
            profileService.SetCulture(preferences, cookieCulture.Name);
            await profileService.UpdateProfileAsync(preferences);
        }

        return cookieCulture;
    }

    private async Task<CultureInfo?> GetCultureFromCookieAsync()
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
                "/_content/Vitarish.Accounts.Interface/Components/Account/CultureSelector.razor.js")
            .AsTask();
}
