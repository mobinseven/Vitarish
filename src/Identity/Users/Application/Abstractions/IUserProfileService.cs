namespace Vitarish.Users.Application.Abstractions;

/// <summary>
/// Application-level contract for working with user profile preferences without requiring HTTP.
/// </summary>
public interface IUserProfileService
{
    Task<Dictionary<string, string>> GetProfileAsync(string userId, CancellationToken ct = default);

    Task UpdateProfileAsync(string userId, Dictionary<string, string> preferences, CancellationToken ct = default);
}
