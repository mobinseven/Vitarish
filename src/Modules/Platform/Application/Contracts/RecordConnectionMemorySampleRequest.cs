using Vitarish.Platform.Domain.Entities;

namespace Vitarish.Platform.Application.Contracts;

public sealed record RecordConnectionMemorySampleRequest(
    string CircuitId,
    string? UserId,
    string? TenantId,
    string? ClientId,
    ConnectionMemorySampleKind Kind,
    DateTimeOffset ObservedAtUtc,
    long ProcessPrivateMemoryBytes,
    long ProcessWorkingSetBytes,
    long ManagedHeapBytes,
    long? EstimatedCircuitDeltaBytes,
    string? FailureReason);
