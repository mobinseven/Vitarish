using Vitarish.Clinic.Application.Assistant;
using Vitarish.Clinic.Application.Assistant.Capabilities;

namespace Vitarish.Clinic.Agents;

public sealed record AssistantCompositionContext(
    AssistantContext Context,
    IReadOnlyCollection<IAssistantCapability> AvailableCapabilities,
    DateTimeOffset CurrentUtc);
