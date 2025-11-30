namespace Vitarish.Identity.Core.Domain.Options;

/// <summary>
/// Options for personal access token configuration.
/// </summary>
public sealed class PersonalAccessTokenOptions
{
    /// <summary>Default lifetime in days for personal access tokens.</summary>
    public int DefaultLifetimeDays { get; init; } = 30;
}
