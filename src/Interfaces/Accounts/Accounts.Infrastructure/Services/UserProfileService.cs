using System.IO;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using ClientUserProfileService = Vitarish.Accounts.Application.Abstractions.IUserProfileService;
using ApplicationUserProfileService = Vitarish.Users.Application.Abstractions.IUserProfileService;

namespace Vitarish.Accounts.Infrastructure.Services;

public class UserProfileService(
    AuthenticationStateProvider authStateProvider,
    ApplicationUserProfileService profileService) : ClientUserProfileService
{
    private const string CulturePreferenceKey = "Culture";

    public async Task<Dictionary<string, string>?> GetProfileAsync()
    {
        AuthenticationState authState = await authStateProvider.GetAuthenticationStateAsync();
        ClaimsPrincipal? user = authState?.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        string? userId = user.FindFirst("sub")?.Value ?? user.Identity?.Name;
        if (string.IsNullOrEmpty(userId))
        {
            return null;
        }

        try
        {
            return await profileService.GetProfileAsync(userId);
        }
        catch (InvalidDataException)
        {
            return null;
        }
    }

    public async Task UpdateProfileAsync(Dictionary<string, string> preferences)
    {
        AuthenticationState authState = await authStateProvider.GetAuthenticationStateAsync();
        if (!authState.User.Identity?.IsAuthenticated ?? true)
        {
            return;
        }

        string? userId = authState.User?.FindFirst("sub")?.Value ?? authState.User?.Identity?.Name;
        if (string.IsNullOrEmpty(userId))
        {
            return;
        }

        try
        {
            await profileService.UpdateProfileAsync(userId, preferences);
        }
        catch (InvalidDataException)
        {
            // Ignore missing profile errors to keep UI flows resilient when the user record is absent.
        }
    }

    public string? GetCulture(IDictionary<string, string> preferences)
    {
        ArgumentNullException.ThrowIfNull(preferences);
        return preferences.TryGetValue(CulturePreferenceKey, out string? culture) ? culture : null;
    }

    public void SetCulture(IDictionary<string, string> preferences, string culture)
    {
        ArgumentNullException.ThrowIfNull(preferences);
        if (string.IsNullOrWhiteSpace(culture))
        {
            return;
        }

        preferences[CulturePreferenceKey] = culture;
    }

    public async Task<string?> GetCultureAsync()
    {
        Dictionary<string, string>? preferences = await GetProfileAsync();
        return preferences is null ? null : GetCulture(preferences);
    }

    public async Task<bool> SetCultureAsync(string culture)
    {
        if (string.IsNullOrWhiteSpace(culture))
        {
            return false;
        }

        Dictionary<string, string>? preferences = await GetProfileAsync();
        if (preferences is null)
        {
            return false;
        }

        SetCulture(preferences, culture);
        await UpdateProfileAsync(preferences);
        return true;
    }
}
