using Vitarish.Users.Application.Abstractions;

namespace Vitarish.Users.Application.ProfileCompletion;

/// <summary>
/// Evaluates profile completion requirements.
/// Baseline requirements:
/// - FirstName (RequiredNow)
/// - LastName (RequiredNow)
/// - DisplayName — derived from FirstName + LastName, not required separately
/// - PreferredLanguage (RequiredLater — not yet in User entity)
/// - TimeZone (RequiredLater — not yet in User entity)
/// - At least one confirmed primary contact: email or phone
/// </summary>
public sealed class ProfileCompletionPolicy(IUsersService usersService) : IProfileCompletionPolicy
{
    public async Task<ProfileCompletionResult> EvaluateAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await usersService.GetUserAsync(userId, cancellationToken);
        if (user is null)
            return ProfileCompletionResult.Incomplete(
                new ProfileCompletionRequirement("User", true, "User not found"));

        var missing = new List<ProfileCompletionRequirement>();

        if (string.IsNullOrWhiteSpace(user.FirstName))
            missing.Add(new ProfileCompletionRequirement("FirstName", true));

        if (string.IsNullOrWhiteSpace(user.LastName))
            missing.Add(new ProfileCompletionRequirement("LastName", true));

        bool hasConfirmedEmail = user.EmailConfirmed == true;
        bool hasConfirmedPhone = user.PhoneNumberConfirmed == true;

        if (!hasConfirmedEmail && !hasConfirmedPhone)
            missing.Add(new ProfileCompletionRequirement("ConfirmedContact", true, "At least one confirmed email or phone is required"));

        if (missing.Count > 0)
            return ProfileCompletionResult.Incomplete([.. missing]);

        return ProfileCompletionResult.Complete;
    }
}
