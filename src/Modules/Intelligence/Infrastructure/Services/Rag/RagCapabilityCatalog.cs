using Vitarish.Intelligence.Application.Abstractions;
using Vitarish.Intelligence.Domain.Models;

namespace Vitarish.Intelligence.Infrastructure.Services.Rag;

public sealed class RagCapabilityCatalog : IRagCapabilityCatalog
{
    private static readonly IReadOnlyList<RagCapabilityDescriptor> Capabilities =
    [
        new(RagCapability.RetrieveOnly, RagCapabilityState.Available, "v1 retrieval-only flow in Intelligence module."),
        new(RagCapability.HealthCheck, RagCapabilityState.Available, "v1 health-check for retrieval readiness."),
        new(RagCapability.GroundedGeneration, RagCapabilityState.Available, "Grounded generation now uses retrieval + responder in Intelligence module.")
    ];

    public IReadOnlyList<RagCapabilityDescriptor> GetCapabilities() => Capabilities;
}
