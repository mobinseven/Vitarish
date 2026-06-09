using Vitarish.Platform.Domain.Entities;

namespace Vitarish.Platform.Application.Contracts;

public sealed record ConnectionMemorySnapshot(
    string CircuitId,
    string? UserId,
    string? TenantId,
    string? ClientId,
    DateTimeOffset LastObservedAtUtc,
    ConnectionMemorySampleKind Kind,
    long ProcessPrivateMemoryBytes,
    long ProcessWorkingSetBytes,
    long ManagedHeapBytes,
    long? EstimatedCircuitDeltaBytes,
    string? FailureReason);
