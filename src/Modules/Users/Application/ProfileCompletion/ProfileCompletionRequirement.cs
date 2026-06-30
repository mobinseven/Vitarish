namespace Vitarish.Users.Application.ProfileCompletion;

/// <summary>
/// A single profile completion requirement.
/// </summary>
public sealed record ProfileCompletionRequirement(
    string FieldName,
    bool IsRequiredNow,
    string? Reason = null
);
