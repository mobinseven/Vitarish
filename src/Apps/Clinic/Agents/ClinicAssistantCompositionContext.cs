using Vitarish.Clinic.Application.ClinicWorkspace;
using Vitarish.Clinic.Application.ClinicWorkspace.Capabilities;
using Vitarish.Intelligence.Application.Agents;

namespace Vitarish.Clinic.Agents;

public sealed record ClinicAssistantCompositionContext(
    ClinicWorkspaceContext Context,
    IReadOnlyCollection<IClinicWorkspaceCapability> AvailableCapabilities,
    IReadOnlyCollection<IIntelligenceAgentTool> IntelligenceTools,
    DateTimeOffset CurrentUtc);
