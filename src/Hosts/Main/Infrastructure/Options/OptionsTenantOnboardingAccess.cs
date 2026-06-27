using Microsoft.Extensions.Options;
using Vitarish.Main.Pages.Common;

namespace Vitarish.Main.Infrastructure.Options;

/// <summary>
/// Adapts infrastructure feature options to the presentation-facing tenant onboarding flag.
/// </summary>
internal sealed class OptionsTenantOnboardingAccess(IOptions<FeaturesOptions> options) : ITenantOnboardingAccess
{
    public bool IsEnabled => options.Value.TenantOnboarding;
}
