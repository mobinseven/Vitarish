using Vitarish.Clinic.Application.ClinicWorkspace.Capabilities;

namespace Vitarish.Clinic.Application.ClinicWorkspace.Policies;

public sealed class ClinicWorkspaceToolAvailabilityPolicy(IReadOnlySet<string>? enabledCapabilities = null)
{
    private readonly IReadOnlySet<string>? _enabledCapabilities = enabledCapabilities;

    public bool IsAvailable(IClinicWorkspaceCapability capability, ClinicWorkspaceContext context) =>
        (_enabledCapabilities is null || _enabledCapabilities.Contains(capability.Name)) && capability.IsAvailable(context);
}
