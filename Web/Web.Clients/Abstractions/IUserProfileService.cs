namespace Vitarish.Web.Clients.Abstractions;

/// <summary>
/// Service for managing user profile preferences.
/// </summary>
public interface IUserProfileService
{
    /// <summary>
    /// Gets the culture from preferences dictionary.
    /// </summary>
    string? GetCulture(IDictionary<string, string> preferences);

    /// <summary>
    /// Gets the current user's culture asynchronously.
    /// </summary>
    Task<string?> GetCultureAsync();

    /// <summary>
    /// Gets the current user's profile preferences.
    /// </summary>
    Task<Dictionary<string, string>?> GetProfileAsync();

    /// <summary>
    /// Sets the culture in preferences dictionary.
    /// </summary>
    void SetCulture(IDictionary<string, string> preferences, string culture);

    /// <summary>
    /// Sets the current user's culture preference.
    /// </summary>
    Task<bool> SetCultureAsync(string culture);

    /// <summary>
    /// Updates the current user's profile preferences.
    /// </summary>
    Task UpdateProfileAsync(Dictionary<string, string> preferences);
}
