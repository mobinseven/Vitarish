namespace Vitarish.Users.Application.ProfileCompletion;

/// <summary>
/// Evaluates whether a user's profile meets completion requirements.
/// Owned by Users/Profile — Authentication must not own profile completion logic.
/// </summary>
public interface IProfileCompletionPolicy
{
    /// <summary>
    /// Evaluates profile completion for the given user ID.
    /// </summary>
    Task<ProfileCompletionResult> EvaluateAsync(string userId, CancellationToken cancellationToken = default);
}
