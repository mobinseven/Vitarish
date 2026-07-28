using Vitarish.Clinic.Application.PatientAgent.Capabilities;

namespace Vitarish.Clinic.Application.PatientAgent.Policies;

public sealed class PatientAgentToolAvailabilityPolicy(IReadOnlySet<string>? enabledCapabilities = null)
{
    private readonly IReadOnlySet<string>? _enabledCapabilities = enabledCapabilities;

    public bool IsAvailable(IPatientAgentCapability capability, PatientAgentContext context) =>
        (_enabledCapabilities is null || _enabledCapabilities.Contains(capability.Name)) && capability.IsAvailable(context);
}
