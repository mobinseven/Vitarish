namespace Vitarish.Intelligence.Domain.Models;

public sealed record RagCapabilityDescriptor(
    RagCapability Capability,
    RagCapabilityState State,
    string Description);
