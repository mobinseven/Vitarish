using Vitarish.Intelligence.Application.Abstractions;
using Vitarish.Intelligence.Domain.Models;

namespace Vitarish.Intelligence.Infrastructure.Services.Rag;

public sealed class RagCapabilityCatalog : IRagCapabilityCatalog
{
    private static readonly IReadOnlyList<RagCapabilityDescriptor> Capabilities =
    [
        new(RagCapability.RetrieveOnly, RagCapabilityState.Available, "v1 retrieval-only flow in Intelligence module."),
        new(RagCapability.HealthCheck, RagCapabilityState.Available, "v1 health-check for retrieval readiness."),
        new(RagCapability.GroundedGeneration, RagCapabilityState.Planned, "Placeholder for grounded generation phase."),
        new(RagCapability.Reranking, RagCapabilityState.Planned, "Placeholder for reranking phase."),
        new(RagCapability.FeedbackLoop, RagCapabilityState.Planned, "Placeholder for feedback loop phase."),
        new(RagCapability.PlatformAdmin, RagCapabilityState.Planned, "PlatformAdmin rollout is intentionally deferred.")
    ];

    public IReadOnlyList<RagCapabilityDescriptor> GetCapabilities() => Capabilities;
}
