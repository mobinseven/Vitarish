namespace Vitarish.Parties.Application.Onboarding;

/// <summary>
/// The tenant of the host where people sign up and create their own clinic. It holds no
/// practice data; it exists so sign-in there has a tenant to bind sessions to, and so
/// sign-ups and their audit records never land in a real clinic.
/// </summary>
public static class OnboardingTenant
{
    public const string OwnerName = "Vitarish Platform";
    public const string AppId = "vitarish-onboarding";
    public const string Name = "Vitarish Onboarding";
}

/// <summary>
/// The onboarding host's public URL (<c>Vitarish:OnboardingUrl</c>). When set, startup binds
/// its host to <see cref="OnboardingTenant"/>; sign-in hands people over to it only while
/// <see cref="TenantOnboardingEnabled"/>.
/// </summary>
public sealed class OnboardingHostOptions
{
    public string? OnboardingUrl { get; set; }

    /// <summary>The <c>Vitarish:Features:TenantOnboarding</c> flag; composition copies it here.</summary>
    public bool TenantOnboardingEnabled { get; set; }
}
