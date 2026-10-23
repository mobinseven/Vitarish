namespace Vitarish.Users.Application.Abstractions;

/// <summary>
/// Application-level contract for working with user profile preferences without requiring HTTP.
/// </summary>
public interface IUserProfileService
{
    Task<Dictionary<string, string>> GetProfileAsync(string userId, CancellationToken ct = default);

    Task UpdateProfileAsync(string userId, Dictionary<string, string> preferences, CancellationToken ct = default);

    /// <summary>
    /// Sets or, with a null value, removes one owner-namespaced entry (<c>owner.name</c>) and leaves the
    /// others as they are. A concurrent change to the same user is reported as a conflict.
    /// </summary>
    Task<Commands.SetUserProfileValueOutcome> SetValueAsync(string userId, string key, string? value, CancellationToken ct = default);
}
