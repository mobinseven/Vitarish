using Microsoft.EntityFrameworkCore;
using Vitarish.Platform.Application.Abstractions;
using Vitarish.Platform.Application.Contracts;
using Vitarish.Platform.Domain.Entities;
using Vitarish.Platform.Infrastructure.Data;

namespace Vitarish.Platform.Infrastructure.Services;

public sealed class PlatformConnectionMemoryStore(
    PlatformDbContext dbContext,
    TimeProvider timeProvider) : IPlatformConnectionMemoryCommand, IPlatformConnectionMemoryQuery
{
    public async Task RecordAsync(
        RecordConnectionMemorySampleRequest request,
        CancellationToken cancellationToken)
    {
        ConnectionMemorySample sample = new()
        {
            Id = Guid.NewGuid(),
            CircuitId = request.CircuitId,
            UserId = request.UserId,
            TenantId = request.TenantId,
            ClientId = request.ClientId,
            ObservedAtUtc = request.ObservedAtUtc,
            Kind = request.Kind,
            ProcessPrivateMemoryBytes = request.ProcessPrivateMemoryBytes,
            ProcessWorkingSetBytes = request.ProcessWorkingSetBytes,
            ManagedHeapBytes = request.ManagedHeapBytes,
            EstimatedCircuitDeltaBytes = request.EstimatedCircuitDeltaBytes,
            FailureReason = request.FailureReason
        };

        sample.ValidateInvariants();

        _ = dbContext.ConnectionMemorySamples.Add(sample);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ConnectionMemorySnapshot>> GetRecentSamplesAsync(
        int limit,
        CancellationToken cancellationToken)
    {
        List<ConnectionMemorySample> samples = await dbContext.ConnectionMemorySamples
            .AsNoTracking()
            .OrderByDescending(e => e.ObservedAtUtc)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return samples
            .Select(ToSnapshot)
            .ToList();
    }

    public async Task<IReadOnlyList<ConnectionMemorySnapshot>> GetCircuitSamplesAsync(
        string circuitId,
        CancellationToken cancellationToken)
    {
        List<ConnectionMemorySample> samples = await dbContext.ConnectionMemorySamples
            .AsNoTracking()
            .Where(e => e.CircuitId == circuitId)
            .OrderByDescending(e => e.ObservedAtUtc)
            .ToListAsync(cancellationToken);

        return samples
            .Select(ToSnapshot)
            .ToList();
    }

    private static ConnectionMemorySnapshot ToSnapshot(ConnectionMemorySample sample)
        => new(
            sample.CircuitId,
            sample.UserId,
            sample.TenantId,
            sample.ClientId,
            sample.ObservedAtUtc,
            sample.Kind,
            sample.ProcessPrivateMemoryBytes,
            sample.ProcessWorkingSetBytes,
            sample.ManagedHeapBytes,
            sample.EstimatedCircuitDeltaBytes,
            sample.FailureReason);
}
