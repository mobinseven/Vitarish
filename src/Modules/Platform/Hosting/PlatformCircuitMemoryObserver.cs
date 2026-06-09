using Microsoft.AspNetCore.Components.Server.Circuits;
using Vitarish.Platform.Application.Abstractions;
using Vitarish.Platform.Application.Contracts;
using Vitarish.Platform.Domain.Entities;
using Vitarish.Platform.Infrastructure.Services;

namespace Vitarish.Platform.Hosting;

public sealed class PlatformCircuitMemoryObserver : CircuitHandler
{
    private readonly IPlatformConnectionMemoryCommand _command;
    private readonly ProcessMemorySampler _sampler;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<PlatformCircuitMemoryObserver> _logger;

    public PlatformCircuitMemoryObserver(
        IPlatformConnectionMemoryCommand command,
        ProcessMemorySampler sampler,
        TimeProvider timeProvider,
        ILogger<PlatformCircuitMemoryObserver> logger)
    {
        _command = command;
        _sampler = sampler;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public override async Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        await RecordSampleAsync(circuit, ConnectionMemorySampleKind.Opened, cancellationToken);
    }

    public override async Task OnCircuitClosedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        await RecordSampleAsync(circuit, ConnectionMemorySampleKind.Closed, cancellationToken);
    }

    private async Task RecordSampleAsync(Circuit circuit, ConnectionMemorySampleKind kind, CancellationToken cancellationToken)
    {
        try
        {
            (long privateMemoryBytes, long workingSetBytes, long managedHeapBytes) = _sampler.Sample();
            
            RecordConnectionMemorySampleRequest request = new(
                CircuitId: circuit.Id,
                UserId: null,
                TenantId: null,
                ClientId: null,
                Kind: kind,
                ObservedAtUtc: _timeProvider.GetUtcNow(),
                ProcessPrivateMemoryBytes: privateMemoryBytes,
                ProcessWorkingSetBytes: workingSetBytes,
                ManagedHeapBytes: managedHeapBytes,
                EstimatedCircuitDeltaBytes: null,
                FailureReason: null);

            await _command.RecordAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to record memory sample for circuit {CircuitId}", circuit.Id);

            try
            {
                RecordConnectionMemorySampleRequest failureRequest = new(
                    CircuitId: circuit.Id,
                    UserId: null,
                    TenantId: null,
                    ClientId: null,
                    Kind: ConnectionMemorySampleKind.Failed,
                    ObservedAtUtc: _timeProvider.GetUtcNow(),
                    ProcessPrivateMemoryBytes: 0,
                    ProcessWorkingSetBytes: 0,
                    ManagedHeapBytes: 0,
                    EstimatedCircuitDeltaBytes: null,
                    FailureReason: ex.Message);

                await _command.RecordAsync(failureRequest, cancellationToken);
            }
            catch (Exception innerEx)
            {
                _logger.LogError(innerEx, "Failed to record failure sample for circuit {CircuitId}", circuit.Id);
            }
        }
    }
}
