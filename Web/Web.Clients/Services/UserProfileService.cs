using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http.Json;
using Vitarish.Web.Clients.Abstractions;

namespace Vitarish.Web.Clients.Services;

/// <summary>
/// Service for managing user profile preferences.
/// </summary>
public class UserProfileService(AuthenticationStateProvider authStateProvider, IIdentityClientFactory identityClientFactory) : IUserProfileService
{
    private const string CulturePreferenceKey = "Culture";
    private const string BaseUrl = "api/user/";

    private Task<HttpClient> CreateClient(string? clientId = null) =>
        identityClientFactory.CreateClient(BaseUrl, clientId);

    /// <inheritdoc />
    public async Task<Dictionary<string, string>?> GetProfileAsync()
    {
        HttpClient httpClient = await CreateClient();
        AuthenticationState authState = await authStateProvider.GetAuthenticationStateAsync();
        if (authState.User.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        string? userId = authState.User.FindFirst("sub")?.Value ?? authState.User.Identity?.Name;
        if (string.IsNullOrEmpty(userId))
        {
            return null;
        }

        using HttpResponseMessage response = await httpClient.GetAsync($"/api/user/profile/{userId}");
        return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<Dictionary<string, string>>() : [];
    }

    /// <inheritdoc />
    public async Task UpdateProfileAsync(Dictionary<string, string> preferences)
    {
        HttpClient httpClient = await CreateClient();
        AuthenticationState authState = await authStateProvider.GetAuthenticationStateAsync();
        if (authState.User.Identity?.IsAuthenticated != true)
        {
            return;
        }

        string? userId = authState.User.FindFirst("sub")?.Value ?? authState.User.Identity?.Name;
        if (string.IsNullOrEmpty(userId))
        {
            return;
        }

        using HttpResponseMessage response = await httpClient.PutAsJsonAsync($"/api/user/profile/{userId}", preferences);
        _ = response.IsSuccessStatusCode;
    }

    /// <inheritdoc />
    public string? GetCulture(IDictionary<string, string> preferences)
    {
        ArgumentNullException.ThrowIfNull(preferences);
        return preferences.TryGetValue(CulturePreferenceKey, out string? culture) ? culture : null;
    }

    /// <inheritdoc />
    public void SetCulture(IDictionary<string, string> preferences, string culture)
    {
        ArgumentNullException.ThrowIfNull(preferences);
        if (string.IsNullOrWhiteSpace(culture))
        {
            return;
        }

        preferences[CulturePreferenceKey] = culture;
    }

    /// <inheritdoc />
    public async Task<string?> GetCultureAsync()
    {
        Dictionary<string, string>? preferences = await GetProfileAsync();
        return preferences is null ? null : GetCulture(preferences);
    }

    /// <inheritdoc />
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
