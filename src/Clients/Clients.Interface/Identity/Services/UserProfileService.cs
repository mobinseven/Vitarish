using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http.Json;
using System.Security.Claims;
using Vitarish.Clients.Identity.Abstractions;
using Vitarish.Clients.Interface.Identity.Abstractions;

namespace Vitarish.Clients.Interface.Identity.Services;

public class UserProfileService(AuthenticationStateProvider authStateProvider, IIdentityClientService aaasClientFactory) : IUserProfileService
{
    private const string CulturePreferenceKey = "Culture";
    private const string BaseUrl = "api/user/";
    private Task<HttpClient> CreateClient(string? clientId = null) => aaasClientFactory.CreateClient(BaseUrl, clientId);
    public async Task<Dictionary<string, string>?> GetProfileAsync()
    {
        HttpClient httpClient = await CreateClient();
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

        HttpResponseMessage response = await httpClient.GetAsync($"/api/user/profile/{userId}");
        return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<Dictionary<string, string>>() : [];
    }

    public async Task UpdateProfileAsync(Dictionary<string, string> preferences)
    {
        HttpClient httpClient = await CreateClient();
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

        _ = await httpClient.PutAsJsonAsync($"/api/user/profile/{userId}", preferences);
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
