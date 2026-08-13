using Vitarish.Clinic.Application.Assistant.Capabilities;

namespace Vitarish.Clinic.Application.Assistant.Policies;

public sealed class AssistantToolAvailabilityPolicy(IReadOnlySet<string>? enabledCapabilities = null)
{
    private readonly IReadOnlySet<string>? _enabledCapabilities = enabledCapabilities;

    public bool IsAvailable(IAssistantCapability capability, AssistantContext context) =>
        (_enabledCapabilities is null || _enabledCapabilities.Contains(capability.Name)) && capability.IsAvailable(context);
}
