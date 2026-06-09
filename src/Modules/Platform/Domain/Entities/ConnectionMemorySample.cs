namespace Vitarish.Platform.Domain.Entities;

public sealed class ConnectionMemorySample
{
    public Guid Id { get; set; }
    public string CircuitId { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string? TenantId { get; set; }
    public string? ClientId { get; set; }
    public DateTimeOffset ObservedAtUtc { get; set; }
    public ConnectionMemorySampleKind Kind { get; set; }
    public long ProcessPrivateMemoryBytes { get; set; }
    public long ProcessWorkingSetBytes { get; set; }
    public long ManagedHeapBytes { get; set; }
    public long? EstimatedCircuitDeltaBytes { get; set; }
    public string? FailureReason { get; set; }

    public void ValidateInvariants()
    {
        if (string.IsNullOrWhiteSpace(CircuitId))
        {
            throw new InvalidOperationException("CircuitId is required.");
        }

        if (ObservedAtUtc.Offset != TimeSpan.Zero)
        {
            throw new InvalidOperationException("ObservedAtUtc must be UTC.");
        }

        if (ProcessPrivateMemoryBytes < 0)
        {
            throw new InvalidOperationException("ProcessPrivateMemoryBytes must be >= 0.");
        }

        if (ProcessWorkingSetBytes < 0)
        {
            throw new InvalidOperationException("ProcessWorkingSetBytes must be >= 0.");
        }

        if (ManagedHeapBytes < 0)
        {
            throw new InvalidOperationException("ManagedHeapBytes must be >= 0.");
        }

        if (EstimatedCircuitDeltaBytes.HasValue && EstimatedCircuitDeltaBytes.Value < 0)
        {
            throw new InvalidOperationException("EstimatedCircuitDeltaBytes must be null or >= 0.");
        }

        if (Kind == ConnectionMemorySampleKind.Failed && string.IsNullOrWhiteSpace(FailureReason))
        {
            throw new InvalidOperationException("Kind == Failed should include FailureReason.");
        }
    }
}
