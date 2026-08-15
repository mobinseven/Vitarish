namespace Vitarish.Main.Infrastructure.Options;

/// <summary>
/// Feature-flag options for toggling in-development functionality.
/// </summary>
public class FeaturesOptions
{
    /// <summary>
    /// Enables the self-service tenant onboarding wizard at /setup.
    /// When false the wizard is inaccessible and the setup button is hidden.
    /// </summary>
    public bool TenantOnboarding { get; set; }

    /// <summary>
    /// Enables social integration module surfaces and webhook endpoints.
    /// </summary>
    public bool SocialIntegrations { get; set; }

    /// <summary>
    /// Enables optional waitlist actions in owner clinic check-in when no same-day seats are available.
    /// </summary>
    public bool CheckInWaitlist { get; set; }
}

