using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.Extensions.Logging;
using Vitarish.Platform.Application.Abstractions;
using Vitarish.Platform.Application.Contracts;
using Vitarish.Platform.Domain.Entities;

namespace Vitarish.Platform.Hosting;

public sealed class PlatformCircuitMemoryObserver : CircuitHandler
{
    private readonly IPlatformConnectionMemoryCommand _command;
    private readonly IConnectionMemorySampler _sampler;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<PlatformCircuitMemoryObserver> _logger;

    public PlatformCircuitMemoryObserver(
        IPlatformConnectionMemoryCommand command,
        IConnectionMemorySampler sampler,
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
            ProcessMemorySample sample = _sampler.Sample();

            RecordConnectionMemorySampleRequest request = new(
                CircuitId: circuit.Id,
                UserId: null,
                TenantId: null,
                ClientId: null,
                Kind: kind,
                ObservedAtUtc: _timeProvider.GetUtcNow(),
                ProcessPrivateMemoryBytes: sample.PrivateMemoryBytes,
                ProcessWorkingSetBytes: sample.WorkingSetBytes,
                ManagedHeapBytes: sample.ManagedHeapBytes,
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
                    FailureReason: BoundFailureReason(ex));

                await _command.RecordAsync(failureRequest, cancellationToken);
            }
            catch (Exception innerEx)
            {
                _logger.LogError(innerEx, "Failed to record failure sample for circuit {CircuitId}", circuit.Id);
            }
        }
    }

    private static string BoundFailureReason(Exception exception)
    {
        string failureReason = string.IsNullOrWhiteSpace(exception.Message)
            ? exception.GetType().Name
            : exception.Message;

        if (failureReason.Length <= ConnectionMemorySample.FailureReasonMaxLength)
        {
            return failureReason;
        }

        return failureReason[..ConnectionMemorySample.FailureReasonMaxLength];
    }
}
