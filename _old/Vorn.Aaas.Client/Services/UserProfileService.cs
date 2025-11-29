using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http.Json;
using Vorn.Aaas.Client.Api.Abstractions;

namespace Vorn.Aaas.Client.Services;

public class UserProfileService(AuthenticationStateProvider authStateProvider, IAaasClientFactory aaasClientFactory)
{
    private const string CulturePreferenceKey = "Culture";
    private const string BaseUrl = "/api/users/";
    private Task<HttpClient> CreateClient(string? clientId = null) => aaasClientFactory.CreateClient(BaseUrl, clientId);
    public async Task<Dictionary<string, string>?> GetProfileAsync()
    {
        HttpClient httpClient = await CreateClient();
        AuthenticationState authState = await authStateProvider.GetAuthenticationStateAsync();
        if (!authState.User.Identity?.IsAuthenticated ?? true)
        {
            return null;
        }

        string? userId = authState.User.FindFirst("sub")?.Value ?? authState.User.Identity.Name;
        if (string.IsNullOrEmpty(userId))
        {
            return null;
        }

        HttpResponseMessage response = await httpClient.GetAsync($"profile/{userId}");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        }
        return new Dictionary<string, string>();
    }

    public async Task UpdateProfileAsync(Dictionary<string, string> preferences)
    {
        HttpClient httpClient = await CreateClient();
        AuthenticationState authState = await authStateProvider.GetAuthenticationStateAsync();
        if (!authState.User.Identity?.IsAuthenticated ?? true)
        {
            return;
        }

        string? userId = authState.User.FindFirst("sub")?.Value ?? authState.User.Identity.Name;
        if (string.IsNullOrEmpty(userId))
        {
            return;
        }

        await httpClient.PutAsJsonAsync($"profile/{userId}", preferences);
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
