namespace Vitarish.Users.Application.ProfileCompletion;

/// <summary>
/// Result of evaluating profile completion requirements.
/// </summary>
public sealed record ProfileCompletionResult(
    bool IsComplete,
    IReadOnlyList<ProfileCompletionRequirement> MissingRequirements
)
{
    public static ProfileCompletionResult Complete { get; } = new(true, []);

    public static ProfileCompletionResult Incomplete(params ProfileCompletionRequirement[] missing)
        => new(false, missing);
}
