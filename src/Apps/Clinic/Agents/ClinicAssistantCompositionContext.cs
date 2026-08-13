using Vitarish.Clinic.Application.Assistant;
using Vitarish.Clinic.Application.Assistant.Capabilities;
using Vitarish.Intelligence.Application.Agents;

namespace Vitarish.Clinic.Agents;

public sealed record ClinicAssistantCompositionContext(
    AssistantContext Context,
    IReadOnlyCollection<IAssistantCapability> AvailableCapabilities,
    IReadOnlyCollection<IIntelligenceAgentTool> IntelligenceTools,
    DateTimeOffset CurrentUtc);
