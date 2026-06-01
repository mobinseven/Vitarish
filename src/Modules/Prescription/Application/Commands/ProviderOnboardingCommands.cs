using MediatR;
using Vitarish.Prescription.Domain.Enums;

namespace Vitarish.Prescription.Application.Commands;

/// <summary>Registers a new insurance provider in Disabled state.</summary>
public sealed record RegisterProviderCommand : IRequest<RegisterProviderResult>
{
    public required string ProviderKey { get; init; }
    public required string Name { get; init; }
    public string? ProviderType { get; init; }
    public IntegrationMode IntegrationMode { get; init; }
}

public sealed record RegisterProviderResult
{
    public bool IsSuccess { get; init; }
    public Guid? ProviderId { get; init; }
    public string? ErrorMessage { get; init; }

    public static RegisterProviderResult Success(Guid id) => new() { IsSuccess = true, ProviderId = id };
    public static RegisterProviderResult Failure(string error) => new() { IsSuccess = false, ErrorMessage = error };
}

/// <summary>Updates provider identity fields.</summary>
public sealed record UpdateProviderCommand : IRequest<UpdateProviderResult>
{
    public required Guid ProviderId { get; init; }
    public required string Name { get; init; }
    public string? ProviderType { get; init; }
    public IntegrationMode IntegrationMode { get; init; }
}

public sealed record UpdateProviderResult
{
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }

    public static UpdateProviderResult Success() => new() { IsSuccess = true };
    public static UpdateProviderResult Failure(string error) => new() { IsSuccess = false, ErrorMessage = error };
}

/// <summary>Bulk-replaces capabilities for a provider.</summary>
public sealed record SetProviderCapabilitiesCommand : IRequest<SetProviderCapabilitiesResult>
{
    public required Guid ProviderId { get; init; }
    public required IReadOnlyList<InsuranceProviderCapabilityType> Capabilities { get; init; }
}

public sealed record SetProviderCapabilitiesResult
{
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }

    public static SetProviderCapabilitiesResult Success() => new() { IsSuccess = true };
    public static SetProviderCapabilitiesResult Failure(string error) => new() { IsSuccess = false, ErrorMessage = error };
}

/// <summary>Transitions provider between Disabled / Sandbox / Active.</summary>
public sealed record SetProviderStatusCommand : IRequest<SetProviderStatusResult>
{
    public required Guid ProviderId { get; init; }
    public required ProviderStatus NewStatus { get; init; }
}

public sealed record SetProviderStatusResult
{
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }

    public static SetProviderStatusResult Success() => new() { IsSuccess = true };
    public static SetProviderStatusResult Failure(string error) => new() { IsSuccess = false, ErrorMessage = error };
}

/// <summary>Tests adapter connectivity for a provider (auth + basic reference data fetch).</summary>
public sealed record TestProviderConnectivityCommand : IRequest<TestProviderConnectivityResult>
{
    public required string ProviderKey { get; init; }
}

public sealed record TestProviderConnectivityResult
{
    public bool IsSuccess { get; init; }
    public long LatencyMs { get; init; }
    public string? ErrorMessage { get; init; }

    public static TestProviderConnectivityResult Success(long latencyMs) => new() { IsSuccess = true, LatencyMs = latencyMs };
    public static TestProviderConnectivityResult Failure(string error) => new() { IsSuccess = false, ErrorMessage = error };
}

/// <summary>Gets all registered providers with status for management grid.</summary>
public sealed record GetProviderManagementGridQuery : IRequest<IReadOnlyList<ProviderManagementDto>>
{
}

public sealed record ProviderManagementDto
{
    public Guid ProviderId { get; init; }
    public string ProviderKey { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? ProviderType { get; init; }
    public IntegrationMode IntegrationMode { get; init; }
    public ProviderStatus Status { get; init; }
    public int CapabilityCount { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
}
