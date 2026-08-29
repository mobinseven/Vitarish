namespace Vitarish.Authentication.Infrastructure.Options;

/// <summary>
/// Trusted host configuration for authentication attempt-key derivation.
/// CurrentPepper is a secret supplied by the deployment secret store. During
/// rotation, PreviousPepper may remain configured only for the maximum lifetime
/// of existing challenge and denial records; remove it after that window.
/// </summary>
public sealed class AuthenticationAttemptKeyOptions
{
    public const string SectionName = "Vitarish:Security:AuthenticationAttempts";
    public const string OpenApiGenerationEnvironmentVariable = "VITARISH_OPENAPI_GENERATION";

    public string Realm { get; set; } = string.Empty;

    public string CurrentPepper { get; set; } = string.Empty;

    public string CurrentVersion { get; set; } = "v1";

    public string? PreviousPepper { get; set; }

    public string? PreviousVersion { get; set; }
}
