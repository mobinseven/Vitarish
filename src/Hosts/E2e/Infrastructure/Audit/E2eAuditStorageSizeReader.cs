using Microsoft.Extensions.Options;
using Vitarish.Audit.Application;
using Vitarish.Audit.Server;

namespace Vitarish.E2e.Infrastructure.Audit;

/// <summary>
/// Whether the E2E host currently reports audit storage above its warning threshold. It is set only for the
/// duration of one capacity check triggered by the audit-evidence endpoint, so other screens are unaffected.
/// </summary>
internal sealed class E2eAuditCapacitySimulation
{
    private volatile bool thresholdExceeded;

    public bool ThresholdExceeded
    {
        get => thresholdExceeded;
        set => thresholdExceeded = value;
    }
}

internal sealed class E2eAuditStorageSizeReader(
    IOptions<SecurityAuditOptions> options,
    E2eAuditCapacitySimulation simulation,
    E2eAuditStoreOutageSimulation outage) : IAuditStorageSizeReader
{
    public Task<long> GetStorageSizeBytesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (outage.Unavailable)
            throw new InvalidOperationException("The E2E audit store is simulating an outage.");

        long threshold = options.Value.CapacityWarningBytes;
        return Task.FromResult(simulation.ThresholdExceeded && threshold is > 0 and < long.MaxValue ? threshold + 1 : 0);
    }
}
