namespace Vitarish.Main.Pages.Common;

/// <summary>
/// Presentation-facing access to the tenant onboarding availability flag.
/// </summary>
public interface ITenantOnboardingAccess
{
    /// <summary>
    /// Gets whether the setup page is available to presentation code.
    /// </summary>
    bool IsEnabled { get; }
}
