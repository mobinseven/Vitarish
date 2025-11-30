using System.Globalization;

namespace Vitarish.Web.Clients.Abstractions;

/// <summary>
/// Provides culture information from user profile.
/// </summary>
public interface IUserProfileCultureInfoProvider
{
    /// <summary>
    /// Gets the user's requested culture.
    /// </summary>
    Task<CultureInfo?> GetRequestedCultureAsync();

    /// <summary>
    /// Sets the user's culture preference.
    /// </summary>
    Task<bool> SetCultureAsync(string cultureName);
}
