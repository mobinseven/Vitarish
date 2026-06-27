using Vitarish.Entities.Contracts.Models;

namespace Vitarish.Medical.Application.Commands;

/// <summary>
/// Syncs provider-specific reference codes (e.g. drug or service codes from an insurance provider)
/// into Medical's versioned CodeSystem/CodeSystemRelease structure.
/// The handler creates the CodeSystem if it does not exist, creates a new release for this sync,
/// deactivates the previous active release, and imports all supplied codes.
/// </summary>
public sealed record SyncProviderCodesCommand : IRequest<EntityResult<ImportResult>>
{
    /// <summary>Short identifier for the insurance provider, e.g. "tamin".</summary>
    public required string ProviderKey { get; init; }

    /// <summary>Code system category, e.g. "drug" or "service".</summary>
    public required string CodeSystem { get; init; }

    /// <summary>
    /// Version string from the provider API response. Used as the release name so
    /// identical re-syncs are idempotent.
    /// </summary>
    public required string DataVersion { get; init; }

    /// <summary>Codes fetched from the provider to import.</summary>
    public required IReadOnlyList<SyncProviderCodeItem> Codes { get; init; }
}

public sealed record SyncProviderCodeItem(
    string Code,
    string DisplayName,
    string? Category,
    bool IsActive);
