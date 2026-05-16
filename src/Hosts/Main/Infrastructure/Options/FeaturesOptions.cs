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
}
