using Vitarish.Clinic.Application.PatientAgent;
using Vitarish.Clinic.Application.PatientAgent.Capabilities;
using Vitarish.Intelligence.Application.Agents;

namespace Vitarish.Clinic.Agents;

public sealed record ClinicAgentCompositionContext(
    PatientAgentContext Context,
    IReadOnlyCollection<IPatientAgentCapability> AvailableCapabilities,
    IReadOnlyCollection<IIntelligenceAgentTool> IntelligenceTools,
    DateTimeOffset CurrentUtc);
